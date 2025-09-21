"use client"

import * as React from "react"
import { ThemeProvider as NextThemesProvider } from "next-themes"

export function ThemeProvider(
  props: React.ComponentProps<typeof NextThemesProvider>
) {
  // 쿠키가 없으면 시스템 설정을 기본으로 따르며, 이미 layout.tsx에서 속성 지정 중
  return <NextThemesProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange {...props} />
}
