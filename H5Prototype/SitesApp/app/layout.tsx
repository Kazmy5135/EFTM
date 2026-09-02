import type { Metadata, Viewport } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'EFTM · 3D 摄像机 Peek 原型',
  description: 'EFTM 手机竖屏 3D 掩体 Peek、真架枪、假动作与准星射击体验原型。',
  openGraph: {
    title: 'EFTM · 3D 摄像机 Peek 原型',
    description: '手机竖屏 3D 掩体 Peek、真架枪、假动作与准星射击体验原型。',
    images: ['/og.png'],
  },
  twitter: {
    card: 'summary_large_image',
    title: 'EFTM · 3D 摄像机 Peek 原型',
    description: '手机竖屏 3D 掩体 Peek、真架枪、假动作与准星射击体验原型。',
    images: ['/og.png'],
  },
};

export const viewport: Viewport = {
  width: 'device-width',
  initialScale: 1,
  minimumScale: 1,
  maximumScale: 1,
  userScalable: false,
  viewportFit: 'cover',
  themeColor: '#080d0e',
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="zh-CN">
      <body>{children}</body>
    </html>
  );
}
