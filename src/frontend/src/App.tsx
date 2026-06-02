import { Route, Routes } from 'react-router-dom';
import './App.css';
import { AuthProvider } from './auth/AuthContext';
import { AppShell } from './components/AppShell';
import { HistoryPage } from './pages/HistoryPage';
import { HomePage } from './pages/HomePage';
import { JoinTabPage } from './pages/JoinTabPage';
import { LeaderboardPage } from './pages/LeaderboardPage';
import { LoginPage } from './pages/LoginPage';
import { NewTabPage } from './pages/NewTabPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { TabDetailPage } from './pages/TabDetailPage';

function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route element={<AppShell />}>
          <Route index element={<HomePage />} />
          <Route path="login" element={<LoginPage />} />
          <Route path="tabs/new" element={<NewTabPage />} />
          <Route path="tabs/:id" element={<TabDetailPage />} />
          <Route path="join/:token" element={<JoinTabPage />} />
          <Route path="history" element={<HistoryPage />} />
          <Route path="leaderboard" element={<LeaderboardPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </AuthProvider>
  );
}

export default App;
