import QRCode from 'qrcode';
import type { PatientDetailDto, PatientIdentityDto, PatientIdentityResolveDto } from '../api/client';
import { printDocument, escapeHtml, formatDate, maskCpf, shortId } from './printDocument';

export function patientIdentityQrPayload(code: string) {
  return `GTH:${code}`;
}

export async function patientIdentityQrDataUrl(code: string, size = 120) {
  return QRCode.toDataURL(patientIdentityQrPayload(code), {
    width: size,
    margin: 1,
    errorCorrectionLevel: 'M',
  });
}

export async function printPatientIdentityWristband(
  patient: PatientDetailDto | PatientIdentityResolveDto,
  identity: PatientIdentityDto,
) {
  const qr = await patientIdentityQrDataUrl(identity.code, 80);
  const displayName = 'fullName' in patient
    ? (patient.socialName ? `${patient.fullName} (${patient.socialName})` : patient.fullName)
    : (patient.socialName ? `${patient.patientName} (${patient.socialName})` : patient.patientName);

  const mrn = patient.medicalRecordNumber
    ?? ('id' in patient ? shortId(patient.id) : shortId(patient.patientId));

  const body = `
    <div class="wristband-strip">
      <div class="wristband-brand">
        <div class="wristband-brand-label">Paciente</div>
      </div>
      <div class="wristband-content">
        <div class="wristband-name-block">
          <div class="wristband-name">${escapeHtml(displayName)}</div>
        </div>
        <div class="wristband-divider"></div>
        <div class="wristband-meta-col">
          <div class="wristband-meta">Pront. <strong>${escapeHtml(mrn)}</strong></div>
          <div class="wristband-meta">Código <strong>${escapeHtml(identity.code)}</strong></div>
        </div>
      </div>
      <div class="wristband-barcode-wrap">
        <img src="${qr}" alt="QR Code" style="width:72px;height:72px" />
        <div class="wristband-barcode">${escapeHtml(identity.code)}</div>
      </div>
    </div>
    <div class="wristband-footer">Pulseira GTH — escaneie para identificar o paciente no leito</div>
  `;

  printDocument({
    title: `Pulseira — ${displayName}`,
    body,
    pageSize: 'wristband',
    autoPrint: true,
  });
}

export async function printPatientIdentityLabel(
  patient: PatientDetailDto | PatientIdentityResolveDto,
  identity: PatientIdentityDto,
) {
  const qr = await patientIdentityQrDataUrl(identity.code, 100);
  const name = 'fullName' in patient ? patient.fullName : patient.patientName;
  const labelTypeName = identity.identityType === 2
    ? 'Exame'
    : identity.identityType === 3
      ? 'Medicamento'
      : identity.identityType === 4
        ? 'Amostra'
        : 'Identificação';

  const body = `
    <div class="badge-card">
      <div class="badge-header">${escapeHtml(labelTypeName)}</div>
      <div class="field"><strong>Nome</strong><span class="field-value lg">${escapeHtml(name)}</span></div>
      ${identity.labelContext ? `<div class="field"><strong>Referência</strong><span class="field-value">${escapeHtml(identity.labelContext)}</span></div>` : ''}
      <div class="field"><strong>Código</strong><span class="field-value xl">${escapeHtml(identity.code)}</span></div>
      <div style="text-align:center;margin-top:8px"><img src="${qr}" alt="QR" style="width:96px;height:96px" /></div>
      <div class="barcode">${escapeHtml(identity.code)}</div>
    </div>
  `;

  printDocument({ title: `Etiqueta — ${name}`, body, pageSize: 'badge', autoPrint: true });
}

export { formatDate, maskCpf };
