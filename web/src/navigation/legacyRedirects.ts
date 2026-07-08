/**
 * Redireciona rotas antigas para os hubs centrais (/recepcao, /ambulatorio).
 * Retorna null se não houver redirecionamento.
 */
export function resolveLegacyRedirect(pathname: string): string | null {
  const normalized = pathname.replace(/\/$/, '') || '/';

  // Prontuário completo permanece na rota dedicada
  if (/^\/pacientes\/[^/]+\/prontuario/.test(normalized)) {
    return null;
  }

  if (normalized === '/agendamentos' || normalized.startsWith('/agendamentos/')) {
    return normalized.replace(/^\/agendamentos/, '/recepcao/agendamentos');
  }

  if (normalized === '/pacientes' || normalized.startsWith('/pacientes/')) {
    return normalized.replace(/^\/pacientes/, '/recepcao/pacientes');
  }

  if (normalized === '/agenda' || normalized.startsWith('/agenda/')) {
    return normalized.replace(/^\/agenda/, '/recepcao/agendamentos');
  }

  if (normalized === '/consultorios' || normalized.startsWith('/consultorios/')) {
    return normalized.replace(/^\/consultorios/, '/ambulatorio/consultorios');
  }

  return null;
}
