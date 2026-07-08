namespace SistemaHospitalar.Domain.BusinessRules;

/// <summary>
/// RN-FOR-010 — Fornecedores bloqueados não participam de compras.
/// </summary>
public static class SupplierRules
{
    public static void ValidateNotBlocked(bool isActive, bool isBlocked)
    {
        if (!isActive)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.SupplierBlocked}] Fornecedor inativo não pode participar de pedidos de compra.");
        }

        if (isBlocked)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.SupplierBlocked}] Fornecedor bloqueado não pode participar de pedidos de compra.");
        }
    }
}
