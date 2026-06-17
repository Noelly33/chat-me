import amqp, { type Channel, type ChannelModel } from 'amqplib'
import { randomUUID } from 'node:crypto'
import type { MessagePublisher, MensajeEnviadoPayload, IniciarChatIndividualPayload, ChatLeidoPayload } from '../../domain/ports/MessagePublisher.js'

const NAMESPACE = 'Consumer.Messaging.Worker.Domain.Events'

const EXCHANGES = {
  mensajeEnviado:        `${NAMESPACE}:MensajeEnviadoEvent`,
  iniciarChatIndividual: `${NAMESPACE}:IniciarChatIndividualEvent`,
  chatLeido:             `${NAMESPACE}:ChatLeidoEvent`,
} as const

function buildEnvelope(exchangeName: string, message: object): Buffer {
  const now = new Date().toISOString()
  return Buffer.from(JSON.stringify({
    messageId:          randomUUID(),
    requestId:          null,
    correlationId:      null,
    conversationId:     randomUUID(),
    initiatorId:        null,
    sourceAddress:      'rabbitmq://rabbitmq/websocket-server',
    destinationAddress: `rabbitmq://rabbitmq/${exchangeName}`,
    responseAddress:    null,
    faultAddress:       null,
    messageType:        [`urn:message:${exchangeName}`],
    message,
    headers:            {},
    host: {
      machineName:            'websocket-server',
      processName:            'websocket-server',
      processId:              process.pid,
      assembly:               'websocket-server',
      assemblyVersion:        '1.0.0',
      frameworkVersion:       'node',
      massTransitVersion:     '9.0.0',
      operatingSystemVersion: process.platform,
    },
    sentTime:       now,
    expirationTime: null,
  }))
}

export class RabbitMQMessagePublisher implements MessagePublisher {
  private channel!: Channel
  private connection!: ChannelModel

  static async create(url: string): Promise<RabbitMQMessagePublisher> {
    const publisher = new RabbitMQMessagePublisher()
    publisher.connection = await amqp.connect(url)
    publisher.channel    = await publisher.connection.createChannel()

    for (const exchange of Object.values(EXCHANGES)) {
      await publisher.channel.assertExchange(exchange, 'fanout', { durable: true })
    }

    return publisher
  }

  async publishMensajeEnviado(payload: MensajeEnviadoPayload): Promise<void> {
    const exchange = EXCHANGES.mensajeEnviado
    this.channel.publish(exchange, '', buildEnvelope(exchange, payload))
  }

  async publishIniciarChatIndividual(payload: IniciarChatIndividualPayload): Promise<void> {
    const exchange = EXCHANGES.iniciarChatIndividual
    this.channel.publish(exchange, '', buildEnvelope(exchange, payload))
  }

  async publishChatLeido(payload: ChatLeidoPayload): Promise<void> {
    const exchange = EXCHANGES.chatLeido
    this.channel.publish(exchange, '', buildEnvelope(exchange, payload))
  }

  async close(): Promise<void> {
    await this.channel.close()
    await this.connection.close()
  }
}
