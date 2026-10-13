import createClient from 'openapi-fetch'
import type { paths } from './schema'
import { getOwnerId } from './ownerId'

export const api = createClient<paths>({
  baseUrl: import.meta.env.VITE_API_URL as string,
})

export const ownerHeader = () => ({ 'X-Owner-Id': getOwnerId() })