export type PrintJob = {
  title: string;
  html: string;
  autoPrint?: boolean;
  width?: 'md' | 'lg';
};

type Listener = (job: PrintJob | null) => void;

let listener: Listener | null = null;

export function subscribePrintPreview(fn: Listener) {
  listener = fn;
  return () => {
    if (listener === fn) listener = null;
  };
}

export function showPrintPreview(job: PrintJob) {
  if (listener) {
    listener(job);
    return true;
  }
  return false;
}

export function closePrintPreview() {
  listener?.(null);
}
