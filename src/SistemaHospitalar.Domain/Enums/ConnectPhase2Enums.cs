namespace SistemaHospitalar.Domain.Enums;



public enum ConnectTicketCategory

{

    TI = 1,

    Infraestrutura = 2,

    Compras = 3,

    RH = 4,

    Financeiro = 5,

    EngenhariaClinica = 6,

    Manutencao = 7,

}



public enum ConnectTicketStatus

{

    Aberto = 1,

    EmAndamento = 2,

    Aguardando = 3,

    Resolvido = 4,

    Cancelado = 5,

}



public enum ConnectTaskStatus

{

    Aberta = 1,

    EmAndamento = 2,

    Aguardando = 3,

    Concluida = 4,

    Cancelada = 5,

}



public enum WorkflowType

{

    SolicitacaoCompra = 1,

    AprovacaoGenerica = 2,

}



public enum WorkflowInstanceStatus

{

    Pendente = 1,

    Aprovado = 2,

    Rejeitado = 3,

    Cancelado = 4,

}



public enum WorkflowStepStatus

{

    Pendente = 1,

    Aprovado = 2,

    Rejeitado = 3,

}

