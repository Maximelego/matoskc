export type AssetCategory = "picture" | "file" | "text"

export type AssetType = {
    category: AssetCategory
    src: string
    alt?: string
}