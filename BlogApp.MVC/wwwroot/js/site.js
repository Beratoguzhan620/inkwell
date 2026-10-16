// Re-check a field that shows an error as soon as its value changes (typing, pasting or autofill).
// Otherwise the message only disappears when the field loses focus, which happens on mousedown on the
// submit button: the button then moves up and the click can miss it.
document.addEventListener("input", function (event) {
    var field = event.target;
    if (!(field instanceof HTMLElement) || !field.classList.contains("input-validation-error")) return;
    if (!window.jQuery || !field.form || !jQuery(field.form).data("validator")) return;
    jQuery(field).valid();
});
