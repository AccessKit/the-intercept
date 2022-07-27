// Copyright 2022 The AccessKit Authors. All rights reserved.
// Licensed under the Apache License, Version 2.0 (found in
// the LICENSE-APACHE file).

use accesskit::{ActionHandler, TreeUpdate};
use accesskit_windows::{Adapter as WindowsAdapter, SubclassingAdapter};
use lazy_static::lazy_static;
use windows::{core::*, Win32::{Foundation::*, System::{LibraryLoader::GetModuleHandleW, Threading::*}, UI::WindowsAndMessaging::*}};

pub struct Adapter {
    adapter: SubclassingAdapter,
}

impl Adapter {
    pub fn new(
        hwnd: HWND,
        source: Box<dyn FnOnce() -> TreeUpdate>,
        action_handler: Box<dyn ActionHandler>,
    ) -> Self {
        let adapter = WindowsAdapter::new(hwnd, source, action_handler);
        let adapter = SubclassingAdapter::new(adapter);
        Self { adapter }
    }

    pub fn update(&self, update: TreeUpdate) {
        self.adapter.update(update).raise();
    }

    pub fn update_if_active(&self, updater: impl FnOnce() -> TreeUpdate) {
        self.adapter.update_if_active(updater).raise();
    }
}

// The following is loosely based on EventLoopThreadExecutor in winit.

// Double-box because the inner box is fat, and we need a plain pointer.
type InnerBoxedCallback = Box<dyn FnOnce() + Send>;
type BoxedCallback = Box<InnerBoxedCallback>;

extern "system" fn callback_receiver_wnd_proc(window: HWND, message: u32, wparam: WPARAM, lparam: LPARAM) -> LRESULT {
    match message as u32 {
        WM_USER => {
            let callback: BoxedCallback = unsafe { Box::from_raw(lparam.0 as *mut _) };
            callback();
            LRESULT(0)
        }
        _ => unsafe { DefWindowProcW(window, message, wparam, lparam) },
    }
}

lazy_static! {
    static ref WIN32_INSTANCE: HINSTANCE = {
        unsafe { GetModuleHandleW(None) }.unwrap()
    };

    static ref DEFAULT_CURSOR: HCURSOR = {
        unsafe { LoadCursorW(None, IDC_ARROW) }.unwrap()
    };

    static ref CALLBACK_RECEIVER_WINDOW_CLASS_ATOM: u16 = {
        // The following is a combination of the implementation of
        // IntoParam<PWSTR> and the class registration function from winit.
        let class_name_wsz: Vec<_> = "AccessKitCallbackReceiver"
            .encode_utf16()
            .chain(std::iter::once(0))
            .collect();

        let wc = WNDCLASSW {
            hCursor: *DEFAULT_CURSOR,
            hInstance: *WIN32_INSTANCE,
            lpszClassName: PCWSTR(class_name_wsz.as_ptr() as _),
            lpfnWndProc: Some(callback_receiver_wnd_proc),
            ..Default::default()
        };

        let atom = unsafe { RegisterClassW(&wc) };
        if atom == 0 {
            let result: windows::core::Result<()> = Err(Error::from_win32());
            result.unwrap();
        }
        atom
    };
}

pub(crate) struct MainThreadCallbackSender {
    thread_id: u32,
    window: HWND,
}

unsafe impl Send for MainThreadCallbackSender {}
unsafe impl Sync for MainThreadCallbackSender {}

impl MainThreadCallbackSender {
    fn in_main_thread(&self) -> bool {
        let thread_id = unsafe { GetCurrentThreadId() };
        thread_id == self.thread_id
    }

    pub(crate) fn send(&self, f: impl FnOnce() + Send + 'static) {
        if self.in_main_thread() {
            f();
        } else {
            let boxed: InnerBoxedCallback = Box::new(f);
            let boxed: BoxedCallback = Box::new(boxed);
            let raw = Box::into_raw(boxed);
            unsafe { PostMessageW(self.window, WM_USER, WPARAM(0), LPARAM(raw as _)) }.unwrap();
        }
    }
}

pub(crate) struct MainThreadCallbackReceiver {
    window: HWND,
}

impl Drop for MainThreadCallbackReceiver {
    fn drop(&mut self) {
        unsafe { DestroyWindow(self.window) }.unwrap();
    }
}

pub(crate) fn main_thread_callback_channel() -> (MainThreadCallbackSender, MainThreadCallbackReceiver) {
    let window = unsafe {
        CreateWindowExW(
            Default::default(),
            PCWSTR(*CALLBACK_RECEIVER_WINDOW_CLASS_ATOM as usize as _),
            "",
            WS_OVERLAPPED,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            HWND_MESSAGE,
            None,
            *WIN32_INSTANCE,
            std::ptr::null_mut(),
        )
    };
    if window.0 == 0 {
        let result: windows::core::Result<()> = Err(Error::from_win32());
        result.unwrap();
    }

    let thread_id = unsafe { GetCurrentThreadId() };
    let sender = MainThreadCallbackSender { thread_id, window };
    let receiver = MainThreadCallbackReceiver { window };
    (sender, receiver)
}
