import { useAuth } from './auth/authContext';
import { AuthForm } from './components/AuthForm';
import { TaskPage } from './components/TaskPage';

// App artık bir "kapı" (auth gate): oturum durumuna göre hangi ekranın gösterileceğine karar verir.
//
// DİKKAT: Bu, GÜVENLİK değil KULLANICI DENEYİMİDİR. Biri tarayıcıda bu kontrolü atlatıp TaskPage'i gösterse bile
// backend her istekte token'ı doğruladığı için (fallback policy) hiçbir veri alamaz.
// Asıl kapı backend'dedir; frontend sadece doğru ekranı gösterir.
export default function App() {
  const auth = useAuth();

  switch (auth.status) {
    case 'loading':
      // Sayfa açılırken refresh token ile oturum kontrol ediliyor. Bu kısa an için giriş formunu
      // GÖSTERMİYORUZ; aksi halde giriş yapmış kullanıcı her yenilemede formu bir anlığına görürdü (flash).
      return (
        <main className="container">
          <p className="empty" role="status">
            Oturum kontrol ediliyor…
          </p>
        </main>
      );
    case 'anonymous':
      return <AuthForm />;
    case 'authenticated':
      return <TaskPage />;
  }
}
