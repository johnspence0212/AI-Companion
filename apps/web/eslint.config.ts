import { globalIgnores } from 'eslint/config'
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript'
import pluginVue from 'eslint-plugin-vue'
import pluginVitest from '@vitest/eslint-plugin'
import skipFormatting from '@vue/eslint-config-prettier/skip-formatting'
import storybook from 'eslint-plugin-storybook'

const featureGlobs = ['src/views/**/*.{vue,ts}', 'src/modules/**/*.{vue,ts}', 'src/App.vue']

export default defineConfigWithVueTs(
  {
    name: 'app/files-to-lint',
    files: ['**/*.{ts,mts,tsx,vue}'],
  },

  globalIgnores(['**/dist/**', '**/dist-ssr/**', '**/coverage/**', '**/storybook-static/**']),

  pluginVue.configs['flat/essential'],
  vueTsConfigs.recommended,

  {
    name: 'app/shadcn-ui',
    files: ['src/ui/**/*.vue'],
    rules: {
      'vue/multi-word-component-names': 'off',
    },
  },

  {
    name: 'app/template-library-features',
    files: featureGlobs,
    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            {
              name: '@/components',
              message: 'Import app chrome from @/ui (template library barrel).',
            },
          ],
          patterns: [
            {
              group: ['@/components/*', '@/components/*/*'],
              message: 'Import app chrome from @/ui (template library barrel).',
            },
            {
              group: ['@/ui/*', '@/ui/*/*'],
              message:
                'Import primitives and chrome from @/ui only — no deep @/ui/... paths in features.',
            },
          ],
        },
      ],
      'no-restricted-globals': [
        'error',
        {
          name: 'fetch',
          message: 'Use @/api modules or stores — do not call fetch in features.',
        },
      ],
      'vue/no-restricted-syntax': [
        'error',
        {
          selector: "VAttribute[key.name='style']",
          message: 'Inline style is forbidden in features. Extend @/ui or theme tokens.',
        },
        {
          selector: "VAttribute[key.name='class'] VLiteral[value=/\\[/]",
          message:
            'Arbitrary Tailwind values (e.g. bg-[#…]) are forbidden in features. Extend @/ui or theme.css.',
        },
        {
          selector: "VAttribute[key.name='class'] VLiteral[value=/#[0-9a-fA-F]{3,8}/]",
          message: 'Hex colors are forbidden in features. Use theme tokens via @/ui.',
        },
      ],
      'no-restricted-syntax': [
        'error',
        {
          selector: 'Literal[value=/#[0-9a-fA-F]{3,8}/]',
          message: 'Hex colors are forbidden in features. Use theme tokens via @/ui.',
        },
      ],
    },
  },

  {
    ...pluginVitest.configs.recommended,
    files: ['src/**/__tests__/*'],
  },
  skipFormatting,
  ...storybook.configs['flat/recommended'],
)
