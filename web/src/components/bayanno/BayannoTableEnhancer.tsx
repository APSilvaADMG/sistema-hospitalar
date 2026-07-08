import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

/**
 * Aplica classes DataTables Bayanno em tabelas .data-table de qualquer tela.
 */
export function BayannoTableEnhancer() {
  const { pathname } = useLocation();

  useEffect(() => {
    const root = document.documentElement;
    if (root.dataset.brand !== 'bayanno' && root.dataset.brand !== 'feegow') return;

    const tables = document.querySelectorAll<HTMLTableElement>(
      'table.data-table:not(.dataTable), table.table:not(.dataTable)',
    );

    tables.forEach((table) => {
      table.classList.add('dTable', 'responsive', 'dataTable');
      table.setAttribute('cellpadding', '0');
      table.setAttribute('cellspacing', '0');

      table.querySelectorAll('thead th').forEach((th) => {
        if (!th.querySelector('div')) {
          const label = th.textContent ?? '';
          th.textContent = '';
          const wrap = document.createElement('div');
          wrap.textContent = label;
          th.appendChild(wrap);
        }
      });
    });
  }, [pathname]);

  return null;
}
