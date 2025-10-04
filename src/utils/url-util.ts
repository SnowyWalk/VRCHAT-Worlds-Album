import {DEFAULT_CATEGORY, DEFAULT_PAGE_NUMBER, DEFAULT_PAGE_SIZE} from "@/utils/consts";

export function getCategoryUrl(category: string, pageNumber: number, pageSize: number)
{
    return `/${category}/${pageNumber}/${pageSize}`;
}

export function getDefaultPageUrl()
{
    return getCategoryUrl(DEFAULT_CATEGORY, DEFAULT_PAGE_NUMBER, DEFAULT_PAGE_SIZE);
}