/** Camada global de modais — acima do shell Feegow/Bayanno (topbar ~1100–1320). */
export const MODAL_LAYER_Z_INDEX = 10000;
export const PRINT_PREVIEW_Z_INDEX = 10050;

export function getModalPortalTarget(): HTMLElement {
  const existing = document.getElementById('app-modal-root');
  if (existing) {
    return existing;
  }

  const root = document.createElement('div');
  root.id = 'app-modal-root';
  document.body.appendChild(root);
  return root;
}
