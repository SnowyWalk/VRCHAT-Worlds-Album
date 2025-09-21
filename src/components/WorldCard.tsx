"use client";

import { Card, CardContent, CardFooter, CardHeader } from "@/components/ui/card";

export type WorldCardProps = {
  id: string;
  name: string;
  authorName: string;
  imageUrl: string;
  capacity?: number;
  visits?: number;
  favorites?: number;
};

export default function WorldCard({ id, name, authorName, imageUrl, capacity, visits, favorites }: WorldCardProps) {
  return (
    <Card className="overflow-hidden">
      <CardHeader className="p-0">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={imageUrl}
          alt={name}
          className="w-full h-40 object-cover bg-gray-100"
          loading="lazy"
        />
      </CardHeader>
      <CardContent className="p-4 space-y-1">
        <div className="font-semibold">{name}</div>
        <div className="text-sm text-gray-500">by {authorName}</div>
      </CardContent>
      <CardFooter className="p-4 pt-0 text-xs text-gray-600 flex gap-3">
        {capacity !== undefined && <span>정원 {capacity}</span>}
        {visits !== undefined && <span>방문 {visits.toLocaleString()}</span>}
        {favorites !== undefined && <span>즐겨찾기 {favorites.toLocaleString()}</span>}
      </CardFooter>
    </Card>
  );
}