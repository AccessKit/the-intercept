// Copyright 2022 The AccessKit Authors. All rights reserved.
// Licensed under the Apache License, Version 2.0 (found in
// the LICENSE-APACHE file).

use accesskit::{
    ActionHandler, ActionRequest, DefaultActionVerb, Node, NodeId, Role, Tree, TreeUpdate
};
use std::{
    ffi::CStr,
    num::NonZeroU128,
    os::raw::c_char,
    sync::{Arc, Mutex}
};
use windows::Win32::{
    Foundation::*,
    System::Diagnostics::Debug::Beep,
    UI::WindowsAndMessaging::*
};

mod platform_impl;

pub struct ActionRequestEvent {
    pub request: ActionRequest,
}

struct UnityActionHandler;

impl ActionHandler for UnityActionHandler {
    fn do_action(&self, _request: ActionRequest) {}
}

pub struct Adapter {
    adapter: platform_impl::Adapter,
}

impl Adapter {
    pub fn new(
        hwnd: HWND,
        source: Box<dyn FnOnce() -> TreeUpdate + Send>,
    ) -> Self {
        let action_handler = UnityActionHandler {};
        let adapter = platform_impl::Adapter::new(hwnd, source, Box::new(action_handler));
        Self { adapter }
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
extern fn init(hwnd: HWND) -> bool {
    let adapter = Box::new(Adapter::new(
        hwnd,
        Box::new(move || initial_tree_update()),
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
