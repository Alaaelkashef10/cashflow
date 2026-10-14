import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, ownerHeader } from './client'
import { toApiError } from './errors'
import type { components } from './schema'

type CreateScenarioRequest = components['schemas']['CreateScenarioRequest']
type CreateEntryRequest = components['schemas']['CreateEntryRequest']
type UpdateEntryRequest = components['schemas']['UpdateEntryRequest']

const scenariosKey = ['scenarios'] as const

export function useScenarios() {
  return useQuery({
    queryKey: scenariosKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/v1/scenarios', {
        params: { header: ownerHeader() },
      })
      if (error || !data) throw toApiError(error)
      return data
    },
  })
}

export function useScenario(id: number) {
  return useQuery({
    queryKey: ['scenarios', id],
    enabled: Number.isInteger(id),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/v1/scenarios/{id}', {
        params: { path: { id }, header: ownerHeader() },
      })
      if (error || !data) throw toApiError(error)
      return data
    },
  })
}

export function useCreateScenario() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: CreateScenarioRequest) => {
      const { data, error } = await api.POST('/api/v1/scenarios', {
        params: { header: ownerHeader() },
        body,
      })
      if (error || !data) throw toApiError(error)
      return data
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scenariosKey }),
  })
}

export function useDeleteScenario() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: number) => {
      const { error } = await api.DELETE('/api/v1/scenarios/{id}', {
        params: { path: { id }, header: ownerHeader() },
      })
      if (error) throw toApiError(error)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scenariosKey }),
  })
}

export function useSamples() {
  return useQuery({
    queryKey: ['samples'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/v1/samples')
      if (error || !data) throw toApiError(error)
      return data
    },
  })
}

export function useCreateSample() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (key: string) => {
      const { data, error } = await api.POST('/api/v1/samples/{key}', {
        params: { path: { key }, header: ownerHeader() },
      })
      if (error || !data) throw toApiError(error)
      return data
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scenariosKey }),
  })
}

export function useEntries(scenarioId: number) {
  return useQuery({
    queryKey: ['scenarios', scenarioId, 'entries'],
    enabled: Number.isInteger(scenarioId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/v1/scenarios/{scenarioId}/entries', {
        params: { path: { scenarioId }, header: ownerHeader() },
      })
      if (error || !data) throw toApiError(error)
      return data
    },
  })
}

export function useCreateEntry(scenarioId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: CreateEntryRequest) => {
      const { data, error } = await api.POST('/api/v1/scenarios/{scenarioId}/entries', {
        params: { path: { scenarioId }, header: ownerHeader() },
        body,
      })
      if (error || !data) throw toApiError(error)
      return data
    },
    // Everything about a scenario lives under ['scenarios'], so this refreshes
    // the list, the detail, the entries, and (later) the projection.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scenariosKey }),
  })
}

export function useUpdateEntry() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, body }: { id: number; body: UpdateEntryRequest }) => {
      const { data, error } = await api.PATCH('/api/v1/entries/{id}', {
        params: { path: { id }, header: ownerHeader() },
        body,
      })
      if (error || !data) throw toApiError(error)
      return data
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scenariosKey }),
  })
}

export function useDeleteEntry() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: number) => {
      const { error } = await api.DELETE('/api/v1/entries/{id}', {
        params: { path: { id }, header: ownerHeader() },
      })
      if (error) throw toApiError(error)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scenariosKey }),
  })
}

export function useProjection(scenarioId: number, horizonEnd: string) {
  return useQuery({
    queryKey: ['scenarios', scenarioId, 'projection', horizonEnd],
    enabled: Number.isInteger(scenarioId),
    placeholderData: keepPreviousData, // no flicker while the projection refreshes
    queryFn: async () => {
      const { data, error } = await api.GET('/api/v1/scenarios/{id}/projection', {
        params: {
          path: { id: scenarioId },
          query: horizonEnd ? { horizonEnd } : {},
          header: ownerHeader(),
        },
      })
      if (error || !data) throw toApiError(error)
      return data
    },
  })
}