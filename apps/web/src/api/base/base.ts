import { httpClient } from './client'

export class BaseApiService<T> {
  protected endpoint: string

  constructor(endpoint: string) {
    this.endpoint = endpoint
  }

  getAll(): Promise<T[]> {
    return httpClient.get<T[]>(`/${this.endpoint}`)
  }

  getById(id: number): Promise<T> {
    return httpClient.get<T>(`/${this.endpoint}/${id}`)
  }

  create(entity: Record<string, unknown>): Promise<T> {
    return httpClient.post<T>(`/${this.endpoint}`, entity)
  }

  update(id: number, entity: T): Promise<void> {
    return httpClient.put(`/${this.endpoint}/${id}`, entity)
  }

  delete(id: number): Promise<void> {
    return httpClient.delete(`/${this.endpoint}/${id}`)
  }
}
