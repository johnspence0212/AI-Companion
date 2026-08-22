import type { Component } from 'vue'
import type { RouteRecordRaw } from 'vue-router'
import { companionModule } from './companion'

export interface ModuleNavigationItem {
  label: string
  to: string
  icon: Component
  permission?: string
}

export interface AppModule {
  key: string
  routes: readonly RouteRecordRaw[]
  navigation: readonly ModuleNavigationItem[]
}

/**
 * Product modules are registered at build time.
 */
export const moduleRegistry: readonly AppModule[] = [companionModule]

export const moduleRoutes: RouteRecordRaw[] = moduleRegistry.flatMap((module) => module.routes)
export const moduleNavigation: ModuleNavigationItem[] = moduleRegistry.flatMap(
  (module) => module.navigation,
)
