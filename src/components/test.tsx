import * as React from 'react';
import * as Tooltip from '@radix-ui/react-tooltip';

export default () => (
  <Tooltip.Root>
    <Tooltip.Trigger asChild>
      ✅ a 태그에 Tooltip.Trigger의 props와 동작이 자식 태그인 a로 병합된다.
      <a href="https://www.radix-ui.com/">Radix UI</a>
    </Tooltip.Trigger>
    <Tooltip.Portal>…</Tooltip.Portal>
  </Tooltip.Root>
);