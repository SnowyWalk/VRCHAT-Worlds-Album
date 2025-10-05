export function getPageApi(category: string, page: string, pageSize: string) {
    return `/api/worlddatalist/?category=${category}&page=${page}&pageSize=${pageSize}`
}