import type { TissGuideDto } from '../api/client';

import {

  buildGuidePrintHtmlDocument,

  buildTissGuidePrintBody,

  guideTypeName,

} from './feegowGuidePrintLayout';

import { printDocument } from './printDocument';



export { guideTypeName } from './feegowGuidePrintLayout';



export function buildTissGuidePrintHtml(guide: TissGuideDto) {

  return buildTissGuidePrintBody(guide);

}



export function printTissGuide(guide: TissGuideDto) {

  const body = buildTissGuidePrintHtml(guide);

  const title = `${guide.guideNumber} — ${guideTypeName(guide.guideType)}`;

  const html = buildGuidePrintHtmlDocument(title, body);

  printDocument({

    title,

    body,

    pageSize: 'funi-a4',

    html,

    previewWidth: 'lg',

  });

}



export function exportTissGuidePdf(guide: TissGuideDto) {

  printTissGuide(guide);

}

