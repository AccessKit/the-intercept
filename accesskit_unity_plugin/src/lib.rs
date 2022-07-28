// Copyright 2022 The AccessKit Authors. All rights reserved.
// Licensed under the Apache License, Version 2.0 (found in
// the LICENSE-APACHE file).

use accesskit::{ActionHandler, ActionRequest, TreeUpdate};
use std::{
    ffi::{CStr, CString},
    os::raw::c_char
};
use windows::Win32::{
    Foundation::*,
    UI::WindowsAndMessaging::*
};

mod platform_impl;

type ActionHandlerCallback = extern "system" fn(*const c_char);

struct UnityActionHandler {
    callback: ActionHandlerCallback,
    sender: platform_impl::MainThreadCallbackSender,
}

impl ActionHandler for UnityActionHandler {
    fn do_action(&self, request: ActionRequest) {
        let request = serde_json::to_string(&request).unwrap();
        let request = CString::new(request).unwrap();
        let callback = self.callback;
        self.sender.send(move || {
            let ptr = request.as_ptr();
            std::mem::forget(request);
            callback(ptr);
        });
    }
}

pub struct Adapter {
    adapter: platform_impl::Adapter,
    _callback_receiver: platform_impl::MainThreadCallbackReceiver,
}

impl Adapter {
    pub fn new(
        hwnd: HWND,
        source: Box<dyn FnOnce() -> TreeUpdate + Send>,
        action_handler: ActionHandlerCallback,
    ) -> Self {
        let (callback_sender, callback_receiver) = platform_impl::main_thread_callback_channel();
        let action_handler = UnityActionHandler {
            callback: action_handler,
            sender: callback_sender,
        };
        let adapter = platform_impl::Adapter::new(hwnd, source, Box::new(action_handler));
        Self { adapter, _callback_receiver: callback_receiver }
    }

    pub fn update(&self, update: TreeUpdate) {
        self.adapter.update(update)
    }

    pub fn update_if_active(&self, updater: impl FnOnce() -> TreeUpdate) {
        self.adapter.update_if_active(updater)
    }
}

fn tree_update_from_json(json: *const c_char) -> Option<TreeUpdate> {
    let json = unsafe { CStr::from_ptr(json).to_str() }.ok()?;
    serde_json::from_str::<TreeUpdate>(json).ok()
}

const PROP_NAME: &str = "AccessKitUnityPlugin";

#[no_mangle]
extern fn init(
    hwnd: HWND,
    action_handler: ActionHandlerCallback,
    initial_tree_update: *const c_char
) -> bool {
    let initial_tree_update = match tree_update_from_json(initial_tree_update) {
        Some(tree_update) => tree_update,
        _ => return false
    };
    let adapter = Box::new(Adapter::new(
        hwnd,
        Box::new(move || initial_tree_update),
        action_handler,
    ));
    let ptr = Box::into_raw(adapter);
    if unsafe { SetPropW(hwnd, PROP_NAME, HANDLE(ptr as _)).as_bool() } {
        unsafe {
            ShowWindow(hwnd, SW_HIDE);
            ShowWindow(hwnd, SW_SHOW);
        };
        true
    } else {
        false
    }
}

#[no_mangle]
extern fn push_update(
    hwnd: HWND,
    tree_update: *const c_char,
    force_push: bool
) -> bool {
    let tree_update = match tree_update_from_json(tree_update) {
        Some(tree_update) => tree_update,
        _ => return false
    };
    let handle = unsafe { GetPropW(hwnd, PROP_NAME) };
    let adapter = unsafe { Box::from_raw(handle.0 as *mut Adapter) };
    if force_push {
        adapter.update(tree_update);
    } else {
        adapter.update_if_active(|| tree_update);
    }
    Box::into_raw(adapter);
    true
}

#[no_mangle]
extern fn destroy(hwnd: HWND) {
    let handle = unsafe { RemovePropW(hwnd, PROP_NAME) }.unwrap();
    unsafe { Box::from_raw(handle.0 as *mut Adapter) };
}
