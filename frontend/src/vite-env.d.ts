// import.meta.env için tip tanımı.
// Bu olmasaydı import.meta.env.VITE_API_URL'in tipi "any" olur, yazım hatasını (VITE_API_ULR) yakalayamazdık.
interface ImportMetaEnv {
  readonly VITE_API_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
