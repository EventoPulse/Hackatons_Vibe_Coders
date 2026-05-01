document.addEventListener('click', async function (event) {
    var button = event.target.closest('.social-share-button');
    if (!button) return;

    var url = button.getAttribute('data-share-url') || window.location.href;
    var title = button.getAttribute('data-share-title') || document.title;

    try {
        if (navigator.share) {
            await navigator.share({ title: title, url: url });
            return;
        }

        if (navigator.clipboard) {
            await navigator.clipboard.writeText(url);
            var original = button.innerHTML;
            button.innerHTML = '<i class="bi bi-check2"></i> Copied';
            window.setTimeout(function () { button.innerHTML = original; }, 1600);
        }
    } catch (_) {
        // Sharing can be cancelled by the user; no UI error is needed.
    }
});
