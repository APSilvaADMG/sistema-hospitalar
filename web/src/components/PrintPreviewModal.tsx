import { useEffect, useRef, useState } from 'react';
import { Modal } from './Modal';
import { closePrintPreview, subscribePrintPreview, type PrintJob } from '../utils/printService';
import { printHtmlDocument } from '../utils/professionalReportTemplate';

export function PrintPreviewHost() {
  const [job, setJob] = useState<PrintJob | null>(null);
  const iframeRef = useRef<HTMLIFrameElement>(null);

  useEffect(() => subscribePrintPreview(setJob), []);

  useEffect(() => {
    if (!job?.autoPrint) return;
    const timer = window.setTimeout(() => {
      if (job.html) {
        printHtmlDocument(job.html, job.title);
      } else {
        iframeRef.current?.contentWindow?.focus();
        iframeRef.current?.contentWindow?.print();
      }
    }, 400);
    return () => window.clearTimeout(timer);
  }, [job]);

  function handlePrint() {
    if (job?.html && printHtmlDocument(job.html, job.title)) {
      return;
    }
    iframeRef.current?.contentWindow?.focus();
    iframeRef.current?.contentWindow?.print();
  }
  if (!job) return null;

  return (
    <Modal
      open
      onClose={() => closePrintPreview()}
      title={job.title}
      subtitle="Pré-visualização — a impressão incluirá somente o relatório."
      width={job.width ?? 'md'}
      overlayClassName="print-preview-overlay"
    >
      <div className="print-preview-frame-wrap">
        <iframe
          ref={iframeRef}
          title={job.title}
          className="print-preview-iframe"
          srcDoc={job.html}
        />
      </div>
      <div className="modal-actions" style={{ marginTop: 16 }}>
        <button className="btn btn-secondary" type="button" onClick={() => closePrintPreview()}>
          Fechar
        </button>
        <button className="btn" type="button" onClick={handlePrint}>
          Imprimir
        </button>
      </div>
    </Modal>
  );
}