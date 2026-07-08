using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Serialization;

public static partial class PortugueseEnumRegistry
{
    private static void RegisterOperationsMaps(Dictionary<Type, EnumMap> maps)
    {
        Register(maps,
            (CleaningType.Terminal, "Terminal"),
            (CleaningType.Concurrent, "Concorrente"),
            (CleaningType.Routine, "Rotina"));

        Register(maps,
            (CleaningRequestStatus.Requested, "Solicitada"),
            (CleaningRequestStatus.InProgress, "Em andamento"),
            (CleaningRequestStatus.Completed, "Concluída"),
            (CleaningRequestStatus.Cancelled, "Cancelada"));

        Register(maps,
            (CleaningTriggerReason.Manual, "Manual"),
            (CleaningTriggerReason.Discharge, "Alta"),
            (CleaningTriggerReason.Transfer, "Transferência"),
            (CleaningTriggerReason.Routine, "Rotina"));

        Register(maps,
            (StockRequisitionStatus.Pending, "Pendente"),
            (StockRequisitionStatus.Approved, "Aprovada"),
            (StockRequisitionStatus.Fulfilled, "Atendida"),
            (StockRequisitionStatus.Cancelled, "Cancelada"),
            (StockRequisitionStatus.Denied, "Negada"));

        Register(maps,
            (StockRequisitionPriority.VeryLow, "Muito baixa"),
            (StockRequisitionPriority.Low, "Baixa"),
            (StockRequisitionPriority.Normal, "Normal"),
            (StockRequisitionPriority.High, "Alta"),
            (StockRequisitionPriority.Critical, "Crítica"));

        Register(maps,
            (StockMovementType.Inbound, "Entrada"),
            (StockMovementType.Outbound, "Saída"),
            (StockMovementType.Adjustment, "Ajuste"));

        Register(maps,
            (StockIssueType.Consumption, "Consumo"),
            (StockIssueType.Loss, "Perda"),
            (StockIssueType.Transfer, "Transferência"),
            (StockIssueType.Patient, "Paciente"));

        Register(maps,
            (WardStockMovementType.TransferIn, "Entrada"),
            (WardStockMovementType.TransferOut, "Saída"),
            (WardStockMovementType.Dispense, "Dispensação"),
            (WardStockMovementType.Adjustment, "Ajuste"));

        Register(maps,
            (ProductType.Medication, "Medicamento"),
            (ProductType.Supply, "Material"),
            (ProductType.General, "Geral"),
            (ProductType.Product, "Produto"));

        Register(maps,
            (InventoryLookupType.Category, "Categoria"),
            (InventoryLookupType.Location, "Local"),
            (InventoryLookupType.Manufacturer, "Fabricante"));

        Register(maps,
            (PurchaseOrderStatus.Draft, "Rascunho"),
            (PurchaseOrderStatus.Sent, "Enviado"),
            (PurchaseOrderStatus.PartiallyReceived, "Recebido parcial"),
            (PurchaseOrderStatus.Received, "Recebido"),
            (PurchaseOrderStatus.Cancelled, "Cancelado"));

        Register(maps,
            (PurchaseSector.Pharmacy, "Farmácia"),
            (PurchaseSector.Laboratory, "Laboratório"),
            (PurchaseSector.Imaging, "Imagem"),
            (PurchaseSector.SurgeryCenter, "Centro cirúrgico"),
            (PurchaseSector.Icu, "UTI"),
            (PurchaseSector.Emergency, "Pronto-socorro"),
            (PurchaseSector.Nutrition, "Nutrição"),
            (PurchaseSector.Laundry, "Lavanderia"),
            (PurchaseSector.ClinicalEngineering, "Eng. clínica"),
            (PurchaseSector.InfectionControl, "CCIH"),
            (PurchaseSector.Hospitality, "Hotelaria"),
            (PurchaseSector.Nursing, "Enfermagem"),
            (PurchaseSector.Administration, "Administração"));

        Register(maps,
            (PurchasePriority.Normal, "Normal"),
            (PurchasePriority.Urgent, "Urgente"),
            (PurchasePriority.Critical, "Crítica"));

        Register(maps,
            (AmbulanceStatus.Available, "Disponível"),
            (AmbulanceStatus.Dispatched, "Despachada"),
            (AmbulanceStatus.OnScene, "No local"),
            (AmbulanceStatus.Transporting, "Transportando"),
            (AmbulanceStatus.Maintenance, "Manutenção"));

        Register(maps,
            (AmbulanceDispatchStatus.Requested, "Solicitado"),
            (AmbulanceDispatchStatus.Dispatched, "Despachado"),
            (AmbulanceDispatchStatus.OnScene, "No local"),
            (AmbulanceDispatchStatus.Transporting, "Transportando"),
            (AmbulanceDispatchStatus.Completed, "Concluído"),
            (AmbulanceDispatchStatus.Cancelled, "Cancelado"));

        Register(maps,
            (ParkingSessionStatus.Active, "Ativo"),
            (ParkingSessionStatus.Completed, "Concluído"));

        Register(maps,
            (TransportAssetType.Stretcher, "Maca"),
            (TransportAssetType.Wheelchair, "Cadeira de rodas"),
            (TransportAssetType.ElectricVehicle, "Veículo elétrico"),
            (TransportAssetType.Other, "Outro"));

        Register(maps,
            (TransportAssetStatus.Available, "Disponível"),
            (TransportAssetStatus.InUse, "Em uso"),
            (TransportAssetStatus.Cleaning, "Higienização"),
            (TransportAssetStatus.Maintenance, "Manutenção"));

        Register(maps,
            (TransportLocationType.Emergency, "Pronto-socorro"),
            (TransportLocationType.Icu, "UTI"),
            (TransportLocationType.SurgeryCenter, "Centro cirúrgico"),
            (TransportLocationType.Hospitalization, "Internação"),
            (TransportLocationType.ImagingTomography, "Tomografia"),
            (TransportLocationType.ImagingXray, "Raio-X"),
            (TransportLocationType.Laboratory, "Laboratório"),
            (TransportLocationType.Discharge, "Alta"),
            (TransportLocationType.Other, "Outro"));

        Register(maps,
            (TransportRequestStatus.Queued, "Na fila"),
            (TransportRequestStatus.Accepted, "Aceito"),
            (TransportRequestStatus.InTransit, "Em trânsito"),
            (TransportRequestStatus.Completed, "Concluído"),
            (TransportRequestStatus.Cancelled, "Cancelado"));

        Register(maps,
            (TransportPriority.Normal, "Normal"),
            (TransportPriority.Urgent, "Urgente"));

        Register(maps,
            (ConsultingRoomStatus.Available, "Disponível"),
            (ConsultingRoomStatus.Occupied, "Ocupado"),
            (ConsultingRoomStatus.Maintenance, "Manutenção"));

        Register(maps,
            (HospitalityRoomStatus.Available, "Disponível"),
            (HospitalityRoomStatus.Occupied, "Ocupado"),
            (HospitalityRoomStatus.Cleaning, "Higienização"),
            (HospitalityRoomStatus.Maintenance, "Manutenção"));

        Register(maps,
            (HospitalityBookingStatus.Reserved, "Reservado"),
            (HospitalityBookingStatus.CheckedIn, "Check-in"),
            (HospitalityBookingStatus.CheckedOut, "Check-out"),
            (HospitalityBookingStatus.Cancelled, "Cancelado"));

        Register(maps,
            (MedicalEquipmentStatus.Operational, "Operacional"),
            (MedicalEquipmentStatus.Maintenance, "Manutenção"),
            (MedicalEquipmentStatus.OutOfService, "Fora de serviço"),
            (MedicalEquipmentStatus.CalibrationDue, "Calibração pendente"));

        Register(maps,
            (MaintenanceWorkOrderStatus.Open, "Aberta"),
            (MaintenanceWorkOrderStatus.InProgress, "Em andamento"),
            (MaintenanceWorkOrderStatus.Completed, "Concluída"),
            (MaintenanceWorkOrderStatus.Cancelled, "Cancelada"));

        Register(maps,
            (SecurityIncidentType.AccessDenied, "Acesso negado"),
            (SecurityIncidentType.VisitorIssue, "Problema com visitante"),
            (SecurityIncidentType.AssetAlert, "Alerta de patrimônio"),
            (SecurityIncidentType.Emergency, "Emergência"),
            (SecurityIncidentType.Other, "Outro"),
            (SecurityIncidentType.PatientFall, "Queda de paciente"),
            (SecurityIncidentType.MedicationError, "Erro de medicação"),
            (SecurityIncidentType.ClinicalAdverseEvent, "Evento adverso clínico"),
            (SecurityIncidentType.NearMiss, "Quase erro"));

        Register(maps,
            (SecurityIncidentStatus.Open, "Aberta"),
            (SecurityIncidentStatus.Investigating, "Em investigação"),
            (SecurityIncidentStatus.Resolved, "Resolvida"));

        Register(maps,
            (VisitorLogStatus.Inside, "No hospital"),
            (VisitorLogStatus.Exited, "Saída registrada"));

        Register(maps,
            (LaundryBatchStatus.Collected, "Coletado"),
            (LaundryBatchStatus.Washing, "Lavando"),
            (LaundryBatchStatus.Drying, "Secando"),
            (LaundryBatchStatus.Delivered, "Entregue"));

        Register(maps,
            (LaundryOrigin.Ward, "Enfermaria"),
            (LaundryOrigin.Icu, "UTI"),
            (LaundryOrigin.Surgery, "Centro cirúrgico"),
            (LaundryOrigin.Emergency, "Pronto-socorro"),
            (LaundryOrigin.Other, "Outro"));

        Register(maps,
            (WasteType.Infectious, "Infectante"),
            (WasteType.Sharps, "Perfurocortante"),
            (WasteType.Common, "Comum"),
            (WasteType.Chemical, "Químico"),
            (WasteType.Pharmaceutical, "Farmacêutico"));

        Register(maps,
            (WasteCollectionStatus.Registered, "Registrado"),
            (WasteCollectionStatus.Stored, "Armazenado"),
            (WasteCollectionStatus.PickedUp, "Coletado"),
            (WasteCollectionStatus.Disposed, "Destinado"));

        Register(maps,
            (AccessPersonType.Patient, "Paciente"),
            (AccessPersonType.Companion, "Acompanhante"),
            (AccessPersonType.Employee, "Funcionário"),
            (AccessPersonType.Visitor, "Visitante"),
            (AccessPersonType.Contractor, "Terceirizado"),
            (AccessPersonType.Doctor, "Médico"),
            (AccessPersonType.Nurse, "Enfermeiro"));

        Register(maps,
            (AccessMethod.Facial, "Facial"),
            (AccessMethod.QrCode, "QR Code"),
            (AccessMethod.Rfid, "RFID"),
            (AccessMethod.Password, "Senha"),
            (AccessMethod.Biometric, "Biometria"),
            (AccessMethod.PlateLpr, "Placa (LPR)"));

        Register(maps,
            (AccessDirection.Entry, "Entrada"),
            (AccessDirection.Exit, "Saída"));

        Register(maps,
            (AccessValidationResult.Granted, "Permitido"),
            (AccessValidationResult.Denied, "Negado"),
            (AccessValidationResult.Expired, "Expirado"),
            (AccessValidationResult.WrongZone, "Zona incorreta"),
            (AccessValidationResult.MaxCompanions, "Limite de acompanhantes"),
            (AccessValidationResult.NoAppointment, "Sem agendamento"),
            (AccessValidationResult.OutsideHours, "Fora do horário"));

        Register(maps,
            (AccessCredentialType.QrCode, "QR Code"),
            (AccessCredentialType.Rfid, "RFID"),
            (AccessCredentialType.FacialLinked, "Facial vinculada"));

        Register(maps,
            (AccessCredentialStatus.Active, "Ativa"),
            (AccessCredentialStatus.Revoked, "Revogada"),
            (AccessCredentialStatus.Expired, "Expirada"));

        Register(maps,
            (FacialBiometricStatus.Active, "Ativa"),
            (FacialBiometricStatus.PendingReview, "Pendente de revisão"),
            (FacialBiometricStatus.Revoked, "Revogada"));

        Register(maps,
            (VehicleOwnerCategory.Patient, "Paciente"),
            (VehicleOwnerCategory.Doctor, "Médico"),
            (VehicleOwnerCategory.Employee, "Funcionário"),
            (VehicleOwnerCategory.Visitor, "Visitante"),
            (VehicleOwnerCategory.Contractor, "Terceirizado"));

        Register(maps,
            (KioskTicketType.Consultation, "Consulta"),
            (KioskTicketType.Exam, "Exame"),
            (KioskTicketType.Hospitalization, "Internação"),
            (KioskTicketType.Emergency, "Emergência"),
            (KioskTicketType.Laboratory, "Laboratório"));

        Register(maps,
            (AccessIntegrationCategory.Turnstile, "Catraca"),
            (AccessIntegrationCategory.FacialRecognition, "Reconhecimento facial"),
            (AccessIntegrationCategory.Parking, "Estacionamento"));

        Register(maps,
            (TvDisplayOrientation.Horizontal, "Horizontal"),
            (TvDisplayOrientation.Vertical, "Vertical"));

        Register(maps,
            (TvDisplayStatus.Online, "Online"),
            (TvDisplayStatus.Offline, "Offline"));

        Register(maps,
            (TvMediaType.Image, "Imagem"),
            (TvMediaType.Video, "Vídeo"),
            (TvMediaType.Pdf, "PDF"),
            (TvMediaType.Slideshow, "Slideshow"));

        Register(maps,
            (TvWidgetType.MediaCarousel, "Mídia"),
            (TvWidgetType.QueueCalls, "Chamadas"),
            (TvWidgetType.NewsTicker, "Notícias"),
            (TvWidgetType.Weather, "Clima"),
            (TvWidgetType.Clock, "Relógio"),
            (TvWidgetType.Dashboard, "Indicadores"),
            (TvWidgetType.Announcements, "Avisos"),
            (TvWidgetType.Bulletin, "Mural"),
            (TvWidgetType.Schedule, "Escalas"));

        Register(maps,
            (TvQueueCallMode.TicketOnly, "Somente senha"),
            (TvQueueCallMode.NameAndDestination, "Nome e destino"));

        Register(maps,
            (HospitalEventLogStatus.Pending, "Pendente"),
            (HospitalEventLogStatus.Processed, "Processado"),
            (HospitalEventLogStatus.Failed, "Falhou"),
            (HospitalEventLogStatus.Partial, "Parcial"));

        Register(maps,
            (NotificationType.Info, "Informação"),
            (NotificationType.Warning, "Aviso"),
            (NotificationType.Alert, "Alerta"));
    }
}
