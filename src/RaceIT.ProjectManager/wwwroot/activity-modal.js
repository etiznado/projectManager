export function show(dialog, receiver) {
    dialog.addEventListener("cancel", event => {
        event.preventDefault();
        receiver.invokeMethodAsync("RequestClose");
    });
    if (!dialog.open) dialog.showModal();
}

export function close(dialog) {
    if (dialog.open) dialog.close();
}
