import { useLocation } from 'react-router-dom';
import { FeegowFinanceAccountInsert } from '../components/feegow/finance/FeegowFinanceAccountInsert';
import { FeegowFinanceAccountList } from '../components/feegow/finance/FeegowFinanceAccountList';
import { FeegowFinanceCashSessions } from '../components/feegow/finance/FeegowFinanceCashSessions';
import { FeegowFinanceMiscellaneousReceipts } from '../components/feegow/finance/FeegowFinanceMiscellaneousReceipts';
import { FeegowFinanceClosingPanel } from '../components/feegow/finance/FeegowFinanceClosingPanel';
import { FeegowFinanceFilteredAccounts } from '../components/feegow/finance/FeegowFinanceFilteredAccounts';
import { FeegowFinanceFixedExpenses } from '../components/feegow/finance/FeegowFinanceFixedExpenses';
import { FeegowFinanceScreenLayout } from '../components/feegow/finance/FeegowFinanceScreenLayout';
import { FeegowFinanceSummary } from '../components/feegow/finance/FeegowFinanceSummary';
import {
  FEEGOW_FINANCE_SECTION_TITLES,
  resolveFeegowFinanceSection,
} from '../components/feegow/finance/feegowFinanceNav';

export function FeegowFinanceWorkspacePage() {
  const { pathname } = useLocation();
  const section = resolveFeegowFinanceSection(pathname);

  let content;
  switch (section) {
    case 'resumo':
      content = <FeegowFinanceSummary />;
      break;
    case 'pagar-inserir':
      content = <FeegowFinanceAccountInsert direction={2} />;
      break;
    case 'pagar-listar':
      content = <FeegowFinanceAccountList direction={2} />;
      break;
    case 'pagar-despesas-fixas':
      content = <FeegowFinanceFixedExpenses />;
      break;
    case 'receber-inserir':
      content = <FeegowFinanceAccountInsert direction={1} />;
      break;
    case 'receber-listar':
      content = <FeegowFinanceAccountList direction={1} />;
      break;
    case 'caixas':
      content = <FeegowFinanceCashSessions />;
      break;
    case 'recibos-diversos':
      content = <FeegowFinanceMiscellaneousReceipts />;
      break;
    case 'extratos':
      content = (
        <FeegowFinanceFilteredAccounts
          title={FEEGOW_FINANCE_SECTION_TITLES.extratos}
          description="Movimentações quitadas — visão consolidada de recebimentos e pagamentos."
          settledOnly
          section="extratos"
        />
      );
      break;
    case 'repasses':
      content = (
        <FeegowFinanceFilteredAccounts
          title={FEEGOW_FINANCE_SECTION_TITLES.repasses}
          description="Repasses a profissionais e terceiros (transferências)."
          paymentMethod={5}
          section="repasses"
        />
      );
      break;
    case 'tef':
      content = (
        <FeegowFinanceFilteredAccounts
          title={FEEGOW_FINANCE_SECTION_TITLES.tef}
          description="Transações eletrônicas de fundos (cartão débito/crédito)."
          searchInDescription="tef"
          section="tef"
        />
      );
      break;
    case 'cheques':
      content = (
        <FeegowFinanceFilteredAccounts
          title={FEEGOW_FINANCE_SECTION_TITLES.cheques}
          description="Lançamentos com forma de pagamento cheque."
          searchInDescription="cheque"
          section="cheques"
        />
      );
      break;
    case 'cartoes':
      content = (
        <FeegowFinanceFilteredAccounts
          title={FEEGOW_FINANCE_SECTION_TITLES.cartoes}
          description="Recebimentos e pagamentos com cartão de crédito."
          paymentMethod={4}
          section="cartoes"
        />
      );
      break;
    case 'fechamento':
      content = <FeegowFinanceClosingPanel />;
      break;
    case 'propostas':
      content = (
        <FeegowFinanceFilteredAccounts
          title={FEEGOW_FINANCE_SECTION_TITLES.propostas}
          description="Propostas e orçamentos em aberto."
          direction={1}
          searchInDescription="proposta"
          defaultStatusFilter="open"
          section="propostas"
          allowConvertProposal
        />
      );
      break;
    case 'descontos':
      content = (
        <FeegowFinanceFilteredAccounts
          title={FEEGOW_FINANCE_SECTION_TITLES.descontos}
          description="Contas com desconto parcial aplicado (saldo em aberto após pagamento)."
          partialOnly
          defaultStatusFilter="open"
          section="descontos"
        />
      );
      break;
    case 'honorarios':
      content = (
        <FeegowFinanceFilteredAccounts
          title={FEEGOW_FINANCE_SECTION_TITLES.honorarios}
          description="Honorários médicos e repasses profissionais."
          direction={1}
          searchInDescription="honor"
          defaultStatusFilter="open"
          section="honorarios"
        />
      );
      break;
    default:
      content = <FeegowFinanceSummary />;
  }

  return (
    <FeegowFinanceScreenLayout>
      {content}
    </FeegowFinanceScreenLayout>
  );
}
