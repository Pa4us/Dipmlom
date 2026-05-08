/* ═══════════════════════════════════════════════
   Общежитие ГГТУ — глобальные скрипты
═══════════════════════════════════════════════ */

/**
 * Tom Select — автоинициализация поиска в <select class="ts-select">.
 * Для маленького варианта (form-select-sm) добавьте класс "ts-sm".
 */
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('select.ts-select').forEach(el => {
        try {
            const isSm = el.classList.contains('ts-sm');

            const ts = new TomSelect(el, {
                allowEmptyOption: true,   // placeholder-опция (пустое value) остаётся
                maxOptions: null,         // без лимита отображаемых вариантов
                searchField: ['text'],
                plugins: [],

                render: {
                    no_results() {
                        return '<div class="px-3 py-2 text-muted small">Ничего не найдено</div>';
                    }
                }
            });

            // Для маленьких контролов добавляем класс-маркер на обёртку
            if (isSm) ts.wrapper.classList.add('ts-sm');

        } catch (err) {
            // Если Tom Select не смог инициализироваться — оставляем нативный select
            console.warn('[TomSelect] init failed:', el, err);
        }
    });
});
