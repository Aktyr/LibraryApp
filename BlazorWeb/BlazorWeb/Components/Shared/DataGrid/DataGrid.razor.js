// ============================================================
// DataGrid.razor.js
//
// Collocated JS-модуль для DataGrid.razor.
// Импортируется из компонента как './DataGrid.razor.js'.
//
// Экспортирует:
//   · initColumnResize(table, dotNetRef, minWidths)
//       — включает перетаскивание границ заголовков колонок.
// ============================================================

/**
 * Инициализирует ресайз колонок для указанной таблицы.
 *
 * @param {HTMLTableElement} table
 *        DOM-элемент <table> (передаётся из Blazor через @ref).
 *
 * @param {DotNetObjectReference} dotNetRef
 *        Ссылка на экземпляр DataGrid<TItem>.
 *        По завершении перетаскивания вызывается
 *        dotNetRef.invokeMethodAsync('OnColumnResizeEnd', colIndex, width).
 *
 * @param {number[]} minWidths
 *        Массив минимальных ширин для каждой колонки (по индексу).
 *        Если значения нет — используется 50px.
 */
export function initColumnResize(table, dotNetRef, minWidths = []) {
    // Защита от повторной инициализации: на одной таблице слушатель ставится один раз.
    if (!table || table._dgResizeInit) return;
    table._dgResizeInit = true;

    // Делегирование: ловим mousedown на всём контейнере таблицы,
    // реагируем только если цель — ручка .dg-resizer.
    table.addEventListener('mousedown', (e) => {
        if (e.button !== 0) return;         // только левая кнопка

        const handle = e.target.closest('.dg-resizer');
        if (!handle) return;

        e.preventDefault();
        e.stopPropagation();

        const colIndex = parseInt(handle.dataset.colIndex, 10);
        if (isNaN(colIndex)) return;

        const th = handle.closest('th');
        if (!th) return;

        // Ищем <col> именно по индексу — независимо от наличия
        // колонки-чекбокса или заполнителя.
        const target = table.querySelector(`colgroup > col[data-col-index="${colIndex}"]`);
        if (!target) return;

        const minWidth = Number.isFinite(minWidths[colIndex]) ? minWidths[colIndex] : 50;

        const startX = e.clientX;
        const startWidth = th.getBoundingClientRect().width;

        const onMove = (ev) => {
            const delta = ev.clientX - startX;
            const w = Math.max(minWidth, Math.round(startWidth + delta));
            target.style.width = w + 'px';
        };

        const onUp = () => {
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
            document.body.style.cursor = '';
            document.body.style.userSelect = '';

            const finalWidth = parseFloat(target.style.width) || startWidth;
            dotNetRef.invokeMethodAsync('OnColumnResizeEnd', colIndex, finalWidth);
        };

        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
    }, true); // capture: реагируем раньше возможных обработчиков заголовка
}