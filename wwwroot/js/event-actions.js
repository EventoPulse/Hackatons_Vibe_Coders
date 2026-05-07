(function () {
    var ACTION_PATTERNS = [
        '/Events/Like/',
        '/Events/Unlike/',
        '/Events/Save/',
        '/Events/Unsave/',
        '/Events/Attendance/',
    ];

    function matchesEventAction(url) {
        return ACTION_PATTERNS.some(function (p) { return url.indexOf(p) !== -1; });
    }

    document.addEventListener('submit', function (e) {
        var form = e.target.closest('form');
        if (!form) return;
        var action = form.getAttribute('action') || form.action || '';
        if (!matchesEventAction(action)) return;
        var card = form.closest('.event-card, .evt-card');
        if (!card) return;

        e.preventDefault();
        var btn = form.querySelector('button[type="submit"]');
        if (btn) btn.disabled = true;

        fetch(action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' },
            credentials: 'same-origin',
        })
            .then(function (resp) {
                if (!resp.ok) throw new Error('HTTP ' + resp.status);
                return resp.json();
            })
            .then(function (data) { applyState(card, form, data); })
            .catch(function () { form.submit(); })
            .finally(function () { if (btn) btn.disabled = false; });
    });

    function applyState(card, form, data) {
        if (typeof data.liked === 'boolean') {
            var likeBtn = card.querySelector('.evt-card__action[title="Like"]');
            if (likeBtn) {
                likeBtn.classList.toggle('is-on-liked', data.liked);
                likeBtn.innerHTML = '<i class="bi ' + (data.liked ? 'bi-heart-fill' : 'bi-heart') + '"></i> ' + data.likesCount;
            }
            var likeForm = likeBtn ? likeBtn.closest('form') : form;
            if (likeForm) {
                var nextAction = data.liked ? 'Unlike' : 'Like';
                likeForm.action = likeForm.action.replace(/\/Events\/(Like|Unlike)\//, '/Events/' + nextAction + '/');
            }
        }

        if (typeof data.saved === 'boolean') {
            var saveBtn = card.querySelector('.evt-card__save');
            if (saveBtn) {
                saveBtn.classList.toggle('is-saved', data.saved);
                saveBtn.innerHTML = '<i class="bi ' + (data.saved ? 'bi-bookmark-fill' : 'bi-bookmark') + '"></i>';
            }
            var saveForm = saveBtn ? saveBtn.closest('form') : form;
            if (saveForm) {
                var nextAction = data.saved ? 'Unsave' : 'Save';
                saveForm.action = saveForm.action.replace(/\/Events\/(Save|Unsave)\//, '/Events/' + nextAction + '/');
            }
        }

        if ('attendanceStatus' in data) {
            var goingBtn = card.querySelector('.evt-card__action[title="Going"]');
            var interestedBtn = card.querySelector('.evt-card__action[title="Interested"]');
            var status = data.attendanceStatus;
            if (goingBtn) {
                goingBtn.classList.toggle('is-on-going', status === 'Going');
                goingBtn.innerHTML = '<i class="bi bi-check2-circle"></i> ' + data.goingCount;
            }
            if (interestedBtn) {
                interestedBtn.classList.toggle('is-on-interested', status === 'Interested');
                interestedBtn.innerHTML = '<i class="bi bi-star"></i> ' + data.interestedCount;
            }
        }
    }
})();
