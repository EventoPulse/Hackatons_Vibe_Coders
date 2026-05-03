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

(function () {
    function showToast(message) {
        if (!message) return;
        var toast = document.createElement('div');
        toast.className = 'groove-toast';
        toast.textContent = message;
        document.body.appendChild(toast);
        requestAnimationFrame(function () { toast.classList.add('is-visible'); });
        window.setTimeout(function () {
            toast.classList.remove('is-visible');
            window.setTimeout(function () { toast.remove(); }, 220);
        }, 1800);
    }

    var pending = sessionStorage.getItem('groove:toast');
    if (pending) {
        sessionStorage.removeItem('groove:toast');
        showToast(pending);
    }

    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!form || !form.getAttribute) return;

        var confirmButton = form.querySelector('[data-confirm-key]');
        if (confirmButton) {
            var key = confirmButton.getAttribute('data-confirm-key');
            var lang = (document.documentElement.getAttribute('lang') || localStorage.getItem('appLang') || 'bg').toLowerCase();
            var messages = {
                'workspace.delete.confirm': {
                    bg: 'Сигурен ли си, че искаш да изтриеш този workspace? Публичните страници под него ще бъдат спрени, но историята за билети и плащания ще остане запазена.',
                    en: 'Are you sure you want to delete this workspace? Public pages under it will be disabled, but ticket and payment history will stay preserved.'
                }
            };
            var entry = messages[key];
            if (entry && !window.confirm(entry[lang] || entry.bg)) {
                event.preventDefault();
                return;
            }
        }

        var action = (form.getAttribute('action') || '').toLowerCase();
        if (action.indexOf('/like') >= 0) sessionStorage.setItem('groove:toast', 'Liked');
        if (action.indexOf('/save') >= 0) sessionStorage.setItem('groove:toast', 'Saved');
        if (action.indexOf('/follow') >= 0) sessionStorage.setItem('groove:toast', 'Following updated');
        if (action.indexOf('/shareevent') >= 0) sessionStorage.setItem('groove:toast', 'Shared on your profile');
        if (action.indexOf('/pinevent') >= 0) sessionStorage.setItem('groove:toast', 'Pinned to your profile');
    });
})();
