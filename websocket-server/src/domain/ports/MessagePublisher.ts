export interface MensajeEnviadoPayload {
  MensajeId: string
  ConversacionId: string
  EmisorId: string
  Contenido: string
  TipoMensajeCodigo: string
  _seq?: number
}

export interface IniciarChatIndividualPayload {
  MensajeId: string
  EmisorId: string
  ReceptorId: string
  Contenido: string
  TipoMensajeCodigo: string
  _seq?: number
}

export interface ChatLeidoPayload {
  ConversacionId: string
  UsuarioId: string
  LeidoAt: string
  _seq?: number
}

export interface MessagePublisher {
  publishMensajeEnviado(payload: MensajeEnviadoPayload): Promise<void>
  publishIniciarChatIndividual(payload: IniciarChatIndividualPayload): Promise<void>
  publishChatLeido(payload: ChatLeidoPayload): Promise<void>
  close(): Promise<void>
}
