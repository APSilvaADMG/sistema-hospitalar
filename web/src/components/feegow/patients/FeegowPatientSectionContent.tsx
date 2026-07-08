import type { ReactNode } from 'react';

import { FeegowPatientAnamnesePanel } from './FeegowPatientAnamnesePanel';

import { FeegowPrescriptionPanel } from '../clinical/FeegowPrescriptionPanel';

import { FeegowVaccinationPanel } from '../clinical/FeegowVaccinationPanel';

import { feegowPatientNavLabel } from './feegowPatientNav';

import {

  FeegowPatientAiSummaryPanel,

  FeegowPatientAppointmentsPanel,

  FeegowPatientDiagnosticsPanel,

  FeegowPatientExamOrdersPanel,

  FeegowPatientFilesPanel,

  FeegowPatientImagingPanel,

  FeegowPatientProductsPanel,

  FeegowPatientProposalsPanel,

  FeegowPatientReceiptsPanel,

  FeegowPatientReferralsPanel,

  FeegowPatientReportsPanel,

  FeegowPatientTasksPanel,

  FeegowPatientTextsPanel,

  FeegowPatientTimelinePanel,

} from './FeegowPatientSectionPanels';

import type { PatientDetailDto } from '../../../api/client';



type Props = {

  patientId: string;

  section: string;

  patient: PatientDetailDto;

};



function SectionPanel({ title, children }: { title: string; children: ReactNode }) {

  return (

    <div className="feegow-patient-section-panel">

      <h3 className="feegow-patient-section-panel-title">{title}</h3>

      {children}

    </div>

  );

}



export function FeegowPatientSectionContent({ patientId, section, patient }: Props) {

  const label = feegowPatientNavLabel(section);

  const props = { patientId, patient };



  let content: ReactNode;



  switch (section) {

    case 'resumos-ia':

      content = <FeegowPatientAiSummaryPanel {...props} />;

      break;

    case 'anamnese':

      content = <FeegowPatientAnamnesePanel {...props} />;

      break;

    case 'laudos':

      content = <FeegowPatientReportsPanel {...props} />;

      break;

    case 'diagnosticos':

      content = <FeegowPatientDiagnosticsPanel {...props} />;

      break;

    case 'encaminhamentos':

      content = <FeegowPatientReferralsPanel {...props} />;

      break;

    case 'prescricoes':

      content = <FeegowPrescriptionPanel {...props} />;

      break;

    case 'textos':

      content = <FeegowPatientTextsPanel {...props} />;

      break;

    case 'tarefas':

      content = <FeegowPatientTasksPanel {...props} />;

      break;

    case 'exames':

      content = <FeegowPatientExamOrdersPanel {...props} />;

      break;

    case 'vacinas':

      content = (

        <FeegowVaccinationPanel

          compact

          patientId={patientId}

          patientName={patient.fullName}

        />

      );

      break;

    case 'produtos':

      content = <FeegowPatientProductsPanel {...props} />;

      break;

    case 'timeline':

      content = <FeegowPatientTimelinePanel {...props} />;

      break;

    case 'imagens':

      content = <FeegowPatientImagingPanel {...props} />;

      break;

    case 'arquivos':

      content = <FeegowPatientFilesPanel {...props} />;

      break;

    case 'agendamentos':

      content = <FeegowPatientAppointmentsPanel {...props} />;

      break;

    case 'recibos':

      content = <FeegowPatientReceiptsPanel {...props} />;

      break;

    case 'propostas':

      content = <FeegowPatientProposalsPanel {...props} />;

      break;

    default:

      content = <p className="feegow-patient-section-empty">Seção não disponível.</p>;

  }



  return (

    <SectionPanel title={label}>

      {content}

    </SectionPanel>

  );

}


