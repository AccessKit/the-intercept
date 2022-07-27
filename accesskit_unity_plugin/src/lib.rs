// Copyright 2022 The AccessKit Authors. All rights reserved.
// Licensed under the Apache License, Version 2.0 (found in
// the LICENSE-APACHE file).

use accesskit::{
    ActionHandler, ActionRequest, Node, NodeId, Role, Tree, TreeUpdate
};
use std::{
    ffi::{CStr, CString},
    num::NonZeroU128,
    os::raw::c_char
};
use windows::Win32::{
    Foundation::*,
    UI::WindowsAndMessaging::*
};

mod platform_impl;

struct UnityActionHandler {
    callback: extern "system" fn(*const c_char),
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
        action_handler: extern "system" fn(*const c_char),
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

const WINDOW_TITLE: &str = "Hello world";

const WINDOW_ID: NodeId = NodeId(unsafe { NonZeroU128::new_unchecked(1) });

fn initial_tree_update() -> TreeUpdate {
    let root = Node {
        name: Some(WINDOW_TITLE.into()),
        ..Node::new(WINDOW_ID, Role::Window)
    };
    TreeUpdate {
        nodes: vec![root],
        tree: Some(Tree::new(WINDOW_ID)),
        focus: None,
    }
}

const PROP_NAME: &str = "AccessKitUnityPlugin";

#[no_mangle]
extern fn init(hwnd: HWND, action_handler: extern "system" fn(*const c_char)) -> bool {
    let adapter = Box::new(Adapter::new(
        hwnd,
        Box::new(move || initial_tree_update()),
        action_handler,
    ));
    unsafe {
        SetPropW(hwnd, PROP_NAME, HANDLE(Box::into_raw(adapter) as _))
    }.unwrap();
    unsafe {
        ShowWindow(hwnd, SW_HIDE);
        ShowWindow(hwnd, SW_SHOW);
    };
    true
}

#[no_mangle]
extern fn push_update(hwnd: HWND, tree_update: *const c_char, force: bool) {
    let tree_update = unsafe { CStr::from_ptr(tree_update).to_str() }.unwrap();
    let tree_update: TreeUpdate = serde_json::from_str(tree_update).unwrap();
    let handle = unsafe { GetPropW(hwnd, PROP_NAME) };
    let adapter = unsafe { Box::from_raw(handle.0 as *mut Adapter) };
    if force {
        adapter.update(tree_update);
    } else {
        adapter.update_if_active(|| tree_update);
    }
    Box::into_raw(adapter);
}

#[no_mangle]
extern fn destroy(hwnd: HWND) {
    let handle = unsafe { RemovePropW(hwnd, PROP_NAME) }.unwrap();
    unsafe { Box::from_raw(handle.0 as *mut Adapter) };
}
