import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"

export default function Page() {
  return (
    <main className="mx-auto max-w-lg p-6 space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>shadcn + Tailwind v4</CardTitle>
        </CardHeader>
        <CardContent className="space-x-3">
          <Button>기본 버튼</Button>
          <Button variant="outline">아웃라인</Button>
        </CardContent>
      </Card>
    </main>
  )
}
