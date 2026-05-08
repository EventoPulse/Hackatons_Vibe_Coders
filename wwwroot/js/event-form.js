(function () {
    'use strict';

    function updateRecurrence() {
        var selected = document.querySelector('input[name="RecurrenceType"]:checked');
        var fields = document.querySelector('[data-event-recurring-fields]');
        var weekdays = document.querySelector('[data-event-weekdays]');
        var weekdaysHelp = document.querySelector('[data-event-weekdays-help]');
        var occurrenceVisibility = document.querySelector('[data-event-occurrence-visibility]');
        if (!selected || !fields) return;

        var isRecurring = selected.value !== 'None' && selected.value !== '0';
        fields.classList.toggle('is-visible', isRecurring);
        var isWeekly = selected.value === 'Weekly' || selected.value === '2';
        if (weekdays) {
            weekdays.style.display = isWeekly ? 'flex' : 'none';
        }
        if (weekdaysHelp) {
            weekdaysHelp.style.display = isWeekly ? 'block' : 'none';
        }
        if (occurrenceVisibility) {
            occurrenceVisibility.style.display = isRecurring ? 'block' : 'none';
        }
    }

    function updateTicketing() {
        var selected = document.querySelector('input[name="TicketingMode"]:checked');
        var fields = document.querySelector('[data-event-layout-fields]');
        if (!selected || !fields) return;

        var needsLayout = selected.value !== 'GeneralAdmission' && selected.value !== '0';
        fields.classList.toggle('is-visible', needsLayout);
    }

    function bindGenrePicker() {
        document.querySelectorAll('[data-event-genre-picker]').forEach(function (picker) {
            var max = parseInt(picker.getAttribute('data-max-genres') || '3', 10);
            var hidden = document.querySelector('input[name="Genre"]');
            var status = picker.parentElement ? picker.parentElement.querySelector('[data-genre-picker-status]') : null;

            function sync(changed) {
                var checked = Array.prototype.slice.call(picker.querySelectorAll('input[type="checkbox"]:checked'));
                if (checked.length > max && changed) {
                    changed.checked = false;
                    checked = Array.prototype.slice.call(picker.querySelectorAll('input[type="checkbox"]:checked'));
                    if (status) {
                        status.textContent = 'Можеш да избереш до ' + max + ' жанра.';
                    }
                } else if (status) {
                    status.textContent = '';
                }

                picker.querySelectorAll('.event-genre-option').forEach(function (option) {
                    var input = option.querySelector('input[type="checkbox"]');
                    option.classList.toggle('is-selected', !!input && input.checked);
                });

                if (hidden && checked.length > 0) {
                    hidden.value = checked[0].value;
                }
            }

            picker.addEventListener('change', function (event) {
                var input = event.target.closest('input[type="checkbox"]');
                if (!input) return;
                sync(input);
            });

            sync();
        });
    }

    document.querySelectorAll('input[name="RecurrenceType"]').forEach(function (input) {
        input.addEventListener('change', updateRecurrence);
    });

    document.querySelectorAll('input[name="TicketingMode"]').forEach(function (input) {
        input.addEventListener('change', updateTicketing);
    });

    updateRecurrence();
    updateTicketing();
    bindGenrePicker();
})();
