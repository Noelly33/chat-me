export interface MensajeEnviadoPayload {
  MensajeId: string
  ConversacionId: string
  EmisorId: string
  Contenido: string
  TipoMensajeCodigo: string
}

export interface IniciarChatIndividualPayload {
  MensajeId: string
  EmisorId: string
  ReceptorId: string
  Contenido: string
  TipoMensajeCodigo: string
}

export interface ChatLeidoPayload {
  ConversacionId: string
  UsuarioId: string
  LeidoAt: string
}

export interface MessagePublisher {
  publishMensajeEnviado(payload: MensajeEnviadoPayload): Promise<void>
  publishIniciarChatIndividual(payload: IniciarChatIndividualPayload): Promise<void>
  publishChatLeido(payload: ChatLeidoPayload): Promise<void>
  close(): Promise<void>
}
