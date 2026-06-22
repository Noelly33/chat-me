/**
 * Outbound port — distributed presence tracking across nodes.
 * Uses reference counting so a user is online as long as any
 * session (on any node) is active.
 */
export interface PresenceRepository {
  increment(username: string): Promise<void>
  /** Returns the remaining session count for that user after decrement */
  decrement(username: string): Promise<number>
  getRoster(): Promise<string[]>
}
