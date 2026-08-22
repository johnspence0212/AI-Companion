import type { StorybookConfig } from '@storybook/vue3-vite'

function flattenPlugins(plugins: unknown): Array<{ name?: string }> {
  if (!plugins) return []
  if (!Array.isArray(plugins)) return [plugins as { name?: string }]
  return plugins.flatMap((plugin) => flattenPlugins(plugin))
}

function shouldSkipPlugin(name: string | undefined): boolean {
  if (!name) return false
  return name.startsWith('vite-plugin-pwa') || name.startsWith('vite-plugin-vue-devtools')
}

const config: StorybookConfig = {
  stories: ['../src/ui/**/*.mdx', '../src/ui/**/*.stories.ts'],
  addons: ['@storybook/addon-a11y', '@storybook/addon-docs'],
  framework: {
    name: '@storybook/vue3-vite',
    options: {
      docgen: {
        plugin: 'vue-component-meta',
        tsconfig: 'tsconfig.app.json',
      },
    },
  },
  async viteFinal(viteConfig) {
    const plugins = flattenPlugins(viteConfig.plugins).filter(
      (plugin) => !shouldSkipPlugin(plugin.name),
    )
    return {
      ...viteConfig,
      plugins,
    }
  },
}

export default config
