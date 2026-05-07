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

    document.querySelectorAll('input[name="RecurrenceType"]').forEach(function (input) {
        input.addEventListener('change', updateRecurrence);
    });

    document.querySelectorAll('input[name="TicketingMode"]').forEach(function (input) {
        input.addEventListener('change', updateTicketing);
    });

    updateRecurrence();
    updateTicketing();
})();
