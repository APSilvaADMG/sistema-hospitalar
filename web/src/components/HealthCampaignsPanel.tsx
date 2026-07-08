import { useMemo } from 'react';

import {

  getCurrentMonthCampaigns,

  type HealthCampaignItem,

} from '../data/healthCampaigns';



function CampaignRibbon({ campaign }: { campaign: HealthCampaignItem }) {

  return (

    <div className={`health-campaign-ribbon health-campaign-color-${campaign.color}`}>

      <span className="health-campaign-ribbon-label">{campaign.colorLabel}</span>

      <span className="health-campaign-ribbon-topic">{campaign.topic}</span>

    </div>

  );

}



export function HealthCampaignsPanel() {

  const current = useMemo(() => getCurrentMonthCampaigns(), []);



  return (

    <div className="health-campaigns-panel card-panel appt-panel">

      <div className="card-panel-header">

        Campanha do mês — {current.monthName}

      </div>

      <div className="card-panel-body">

        <p className="health-campaigns-intro">

          Calendário nacional de cores para prevenção, tratamento e bem-estar. Use este quadro

          para orientar pacientes e equipe sobre as ações do mês.

        </p>

        <div className="health-campaigns-current-list">

          {current.campaigns.map((campaign) => (

            <CampaignRibbon key={`${current.month}-${campaign.colorLabel}`} campaign={campaign} />

          ))}

        </div>

        {current.examsHint && (

          <p className="health-campaigns-exams">

            <strong>Exames e ações em destaque:</strong> {current.examsHint}

          </p>

        )}

      </div>

      <div className="card-panel-footer health-campaigns-footer">

        <p>

          Calendários oficiais e unidades de atendimento: consulte o{' '}

          <a href="https://www.gov.br/saude/pt-br" target="_blank" rel="noreferrer">

            Ministério da Saúde

          </a>{' '}

          e a Secretaria Municipal de Saúde da sua região.

        </p>

      </div>

    </div>

  );

}

