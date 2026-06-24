let counter = 0;

export function nextSeq(): number {
  return ++counter;
}

export function log(seq: number, event: string, fields: Record<string, unknown> = {}): void {
  const ts = new Date().toISOString().slice(11, 23);
  const parts = Object.entries(fields)
    .filter(([, v]) => v !== undefined && v !== null)
    .map(([k, v]) => {
      if (typeof v === 'string') return `${k}=${JSON.stringify(v)}`;
      return `${k}=${String(v)}`;
    })
    .join(' ');
  const prefix = `[ws-server] ${ts} [#${seq}] ${event}`;
  console.log(parts ? `${prefix} ${parts}` : prefix);
}
