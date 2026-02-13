/**
 * Post-generation fixup for Orval Zod output.
 *
 * Orval v8 incorrectly generates `.regex()` on `zod.number()` for integer
 * types. This method only exists on `zod.string()`. This script strips the
 * invalid `.regex(...)` calls from number schemas.
 */

import { readFileSync, writeFileSync } from 'node:fs'
import { globSync } from 'node:fs'

const files = globSync('src/api/generated/**/*.zod.ts')

let fixedCount = 0

for (const file of files) {
  const original = readFileSync(file, 'utf-8')
  let content = original

  // Remove .regex(...) from zod.number() — method doesn't exist on ZodNumber
  content = content.replace(/zod\.number\(\)\.regex\([^)]+\)/g, 'zod.number()')

  // Simplify union([zod.number(), zod.string().regex(...)]) to zod.coerce.number()
  content = content.replace(
    /zod\.union\(\[zod\.number\(\),\s*zod\.string\(\)\.regex\([^)]+\)\]\)/g,
    'zod.coerce.number()'
  )

  // Remove now-unused RegExp declarations
  content = content.replace(
    /^export const \w+RegExp(?:One|Two) = new RegExp\([^)]+\);\n/gm,
    ''
  )

  if (content !== original) {
    writeFileSync(file, content)
    fixedCount++
    console.log(`Fixed: ${file}`)
  }
}

if (fixedCount > 0) {
  console.log(
    `\nFixed ${fixedCount} file(s) with Orval zod.number().regex() bug`
  )
} else {
  console.log('No fixes needed')
}
