"use client";

import { useEffect, useState } from "react";
import WorldCard from "@/components/WorldCard";

// 백엔드가 같은 오리진에서 서빙된다고 가정하고 상대 경로 사용
const PAGE_API = "/api/page/0?page_size=12";

export type WorldPayload = {
  id: string;
  name: string;
  authorId: string;
  authorName: string;
  imageUrl: string;
  capacity: number;
  visits: number;
  favorites: number;
  heat: number;
  popularity: number;
  tags: string[];
} | null;

export default function Page() {
  const [worlds, setWorlds] = useState<WorldPayload[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let aborted = false;
    async function load() {
      try {
        setLoading(true);
        setError(null);
        const res = await fetch(PAGE_API, { cache: "no-store" });
        if (!res.ok) {
          throw new Error(`HTTP ${res.status}`);
        }
        const data: WorldPayload[] = await res.json();
        if (!aborted) setWorlds(Array.isArray(data) ? data : []);
      } catch (e: any) {
        if (!aborted) setError(e?.message ?? "요청 실패");
      } finally {
        if (!aborted) setLoading(false);
      }
    }
    load();
    return () => {
      aborted = true;
    };
  }, []);

  const validWorlds = worlds.filter(Boolean) as Exclude<WorldPayload, null>[];

  return (
    <main className="mx-auto max-w-5xl p-6 space-y-6">
      {loading && <p>불러오는 중...</p>}
      {error && (
        <p className="text-red-600">불러오기 오류: {error}</p>
      )}

      <section className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {validWorlds.map((w) => (
          <WorldCard
            key={w.id}
            id={w.id}
            name={w.name}
            authorName={w.authorName}
            imageUrl={w.imageUrl}
            capacity={w.capacity}
            visits={w.visits}
            favorites={w.favorites}
          />
        ))}
      </section>
    </main>
  );
}
