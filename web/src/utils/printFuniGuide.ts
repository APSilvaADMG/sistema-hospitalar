import { formatBrDate } from './dateUtils';

import {

  buildGuidePrintFooter,

  buildGuidePrintHtmlDocument,

  FEEGOW_GUIDE_PRINT_CSS,

} from './feegowGuidePrintLayout';

import { logoImg } from './printDocument';

import { printDocument } from './printDocument';



function canvasToDataUrl(canvas: HTMLCanvasElement): string | null {

  try {

    return canvas.toDataURL('image/png');

  } catch {

    return null;

  }

}



function readControlValue(el: HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement): string {

  if (el instanceof HTMLSelectElement) {

    const option = el.options[el.selectedIndex];

    return option?.text?.trim() || el.value;

  }

  if (el instanceof HTMLTextAreaElement) {

    return el.value;

  }

  if (el.type === 'date' && el.value) {

    return formatBrDate(el.value);

  }

  return el.value;

}



function replaceControlWithPrintValue(

  sourceEl: HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement,

  cloneEl: Element,

) {

  const value = readControlValue(sourceEl);

  const isCharCell = sourceEl.classList.contains('funi-char-cell');

  const isTextarea = sourceEl instanceof HTMLTextAreaElement;



  if (isCharCell) {

    const span = document.createElement('span');

    span.className = 'funi-print-char';

    span.textContent = value || ' ';

    cloneEl.replaceWith(span);

    return;

  }



  const span = document.createElement('span');

  span.className = `funi-print-value${isTextarea ? ' funi-print-value--textarea' : ''}`;

  span.textContent = value;

  cloneEl.replaceWith(span);

}



function normalizeGuideHeaderForPrint(clone: HTMLElement) {

  const header = clone.querySelector('.funi-guide-header');

  if (!(header instanceof HTMLElement)) return;



  let logoSlot = header.querySelector('.funi-guide-logo');

  if (!logoSlot) {

    logoSlot = document.createElement('div');

    logoSlot.className = 'funi-guide-logo';

    header.insertBefore(logoSlot, header.firstChild);

  }

  logoSlot.innerHTML = logoImg(56);



  const brandSlot = header.querySelector('.funi-guide-brand');

  brandSlot?.remove();



  const hospitalBrand = header.querySelector('.funi-guide-hospital-brand');

  hospitalBrand?.remove();



  const titleBlock = header.querySelector('.funi-guide-title')?.parentElement;

  if (titleBlock && !titleBlock.classList.contains('funi-guide-header-main')) {

    const main = document.createElement('div');

    main.className = 'funi-guide-header-main';

    const title = header.querySelector('.funi-guide-title');

    const subtitle = header.querySelector('.funi-guide-subtitle');

    if (title) main.appendChild(title);

    if (subtitle) main.appendChild(subtitle);

    const operator = header.querySelector('.funi-guide-operator');

    header.insertBefore(main, operator ?? header.lastChild);

    if (titleBlock !== main && titleBlock.childElementCount === 0) {

      titleBlock.remove();

    }

  }

}



function prepareGuideSheetForPrint(source: HTMLElement): HTMLElement {

  const clone = source.cloneNode(true) as HTMLElement;



  const sourceControls = source.querySelectorAll('input, select, textarea');

  const cloneControls = clone.querySelectorAll('input, select, textarea');

  cloneControls.forEach((cloneEl, index) => {

    const sourceEl = sourceControls[index];

    if (!sourceEl) return;

    replaceControlWithPrintValue(

      sourceEl as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement,

      cloneEl,

    );

  });



  const sourceCanvases = source.querySelectorAll('canvas.signature-canvas');

  const cloneCanvases = clone.querySelectorAll('canvas.signature-canvas');

  cloneCanvases.forEach((canvasEl, index) => {

    const sourceCanvas = sourceCanvases[index] as HTMLCanvasElement | undefined;

    const pad = canvasEl.closest('.signature-pad');

    if (!pad) return;



    const dataUrl = sourceCanvas ? canvasToDataUrl(sourceCanvas) : null;

    pad.innerHTML = '';

    if (dataUrl) {

      const img = document.createElement('img');

      img.src = dataUrl;

      img.alt = 'Assinatura';

      img.className = 'funi-print-signature';

      pad.appendChild(img);

    } else {

      const line = document.createElement('div');

      line.className = 'funi-print-empty-signature';

      pad.appendChild(line);

    }

  });



  clone.querySelectorAll('.signature-pad-header, .form-hint, button, .card').forEach((el) => el.remove());

  clone.querySelectorAll('.funi-operator-banner').forEach((el) => el.remove());

  clone.querySelectorAll('.funi-guide-hospital-brand').forEach((el) => el.remove());



  normalizeGuideHeaderForPrint(clone);



  const footerWrap = document.createElement('div');
  footerWrap.innerHTML = buildGuidePrintFooter();
  const footerEl = footerWrap.firstElementChild;
  if (footerEl) clone.appendChild(footerEl);

  return clone;

}



export function buildFuniGuidePrintHtml(title: string, sheetHtml: string): string {

  return buildGuidePrintHtmlDocument(title, sheetHtml);

}



export function printFuniGuide(title: string): boolean {

  const sheet = document.querySelector('.funi-guide-sheet');

  if (!(sheet instanceof HTMLElement)) {

    window.alert('Formulário da guia não encontrado. Abra uma guia FUNI preenchida antes de imprimir.');

    return false;

  }



  const prepared = prepareGuideSheetForPrint(sheet);

  const html = buildFuniGuidePrintHtml(title, prepared.outerHTML);



  printDocument({

    title,

    body: prepared.outerHTML,

    pageSize: 'funi-a4',

    autoPrint: false,

    html,

    previewWidth: 'lg',

  });



  return true;

}



export { FEEGOW_GUIDE_PRINT_CSS };

