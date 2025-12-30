# YOUR_SITE_NAME - URL Routing & Navigation Guide

## 🗺️ Complete URL Map

### Frontend Routes

#### Home Pages
| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `/` | Home | Index | Home page with featured manga |
| `/Home/Privacy` | Home | Privacy | Privacy policy page |

#### Series Pages
| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `/Series` | Series | Index | Browse all manga series |
| `/Series/Detail/1` | Series | Detail | View series details |
| `/Series/ReadChapter/5` | Series | ReadChapter | Read chapter pages |

### Admin Routes

#### Dashboard
| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `/Admin` | Admin | Index | Admin dashboard |

#### Manga Management
| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `/Admin/MangaList` | Admin | MangaList | View all manga |
| `/Admin/CreateManga` | Admin | CreateManga | Create new manga |
| `/Admin/EditManga/1` | Admin | EditManga | Edit manga |
| `/Admin/DeleteManga/1` | Admin | DeleteManga | Delete manga |
