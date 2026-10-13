const KEY = 'cashflow.ownerId'
const fallbackId = crypto.randomUUID()

// Demo mode: an anonymous id stored in this browser, sent as X-Owner-Id.
export function getOwnerId(): string {
  try {
    let id = localStorage.getItem(KEY)
    if (!id) {
      id = crypto.randomUUID()
      localStorage.setItem(KEY, id)
    }
    return id
  } catch {
    return fallbackId
  }
}