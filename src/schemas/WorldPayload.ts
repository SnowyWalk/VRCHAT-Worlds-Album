import Dict = NodeJS.Dict;

type WorldMetadata = {
    worldId: string;
    worldName: string;
    authorId: string;
    authorName: string;
    imageUrl: string;
    capacity: number;
    visits: number;
    favorites: number;
    heat: number;
    popularity: number;
    tags: string[];
    updatedAt: Date;
}

export type WorldPayload = {
    worldId: string;
    metadata: WorldMetadata;
    imageList: Dict<string>[];
    category: string[] | null;
    description: string | null;
    dataCreatedAt: Date;
    lastFolderModifiedAt: Date;
} | null;