import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import NavigationBar from '../../components/NavigationBar';
import ScrollToTop from '../../components/ScrollToTop';
import { useLanguage } from '../../hooks/useLanguage';
import '../../styles/HomepageTailwind.css';
import '../../styles/AboutUsTailwind.css';

const AboutUs = () => {
  const navigate = useNavigate();
  const [isLoaded, setIsLoaded] = useState(false);
  const { strings } = useLanguage();

  useEffect(() => {
    setIsLoaded(true);
  }, []);

  return (
    <div className={`min-h-screen bg-slate-900 text-slate-200 flex flex-col relative z-10 ${isLoaded ? 'loaded' : ''}`}>
      {/* Navigation Bar */}
      <NavigationBar activePage="about" />

      {/* Hero Section - Similar to Homepage, but with About Us content */}
      <section className="about-hero relative flex items-center justify-center py-32 px-8 overflow-hidden mt-20 min-h-screen">
        {/* Background Building Animation */}
        <div className="background-buildings absolute inset-0 z-10 pointer-events-none">
          <div className="building-silhouettes absolute inset-0 w-full h-full">
            {/* Large background buildings */}
            <div className="bg-building-1 absolute w-40 h-96 bg-gradient-to-t from-slate-600 to-slate-500 rounded-t-lg opacity-40 shadow-2xl" style={{ right: '8%', top: '15%' }}></div>
            <div className="bg-building-2 absolute w-32 h-80 bg-gradient-to-t from-slate-600 to-slate-500 rounded-t-lg opacity-35 shadow-2xl" style={{ right: '22%', top: '25%' }}></div>
            <div className="bg-building-3 absolute w-36 h-88 bg-gradient-to-t from-slate-600 to-slate-500 rounded-t-lg opacity-38 shadow-2xl" style={{ right: '3%', top: '35%' }}></div>
            
            {/* Floating tech icons */}
            <div className="tech-icon-1 absolute text-5xl opacity-60" style={{ right: '12%', top: '20%' }}>🏢</div>
            <div className="tech-icon-2 absolute text-4xl opacity-55" style={{ right: '18%', top: '40%' }}>🤖</div>
            <div className="tech-icon-3 absolute text-4xl opacity-60" style={{ right: '6%', top: '55%' }}>📊</div>
            <div className="tech-icon-4 absolute text-4xl opacity-55" style={{ right: '28%', top: '30%' }}>☁️</div>
            <div className="tech-icon-5 absolute text-4xl opacity-60" style={{ right: '10%', top: '65%' }}>🔒</div>
            <div className="tech-icon-6 absolute text-4xl opacity-55" style={{ right: '32%', top: '45%' }}>⚡</div>
            
            {/* Connection lines */}
            <div className="connection-line-1 absolute w-40 h-1 bg-gradient-to-r from-transparent via-blue-400 to-transparent opacity-40" style={{ right: '8%', top: '30%' }}></div>
            <div className="connection-line-2 absolute w-32 h-1 bg-gradient-to-r from-transparent via-blue-400 to-transparent opacity-40" style={{ right: '12%', top: '50%' }}></div>
            <div className="connection-line-3 absolute w-36 h-1 bg-gradient-to-r from-transparent via-blue-400 to-transparent opacity-40" style={{ right: '3%', top: '60%' }}></div>
          </div>
        </div>
        
        <div className="hero-content z-20 text-white relative flex items-center gap-16 max-w-7xl w-full">
          <div className="hero-left flex-1 max-w-2xl">
            <div className={`hero-text ${isLoaded ? 'animate-in' : ''}`}>
              <h1 className="text-5xl lg:text-6xl font-extrabold leading-tight mb-6 text-shadow-lg">
                <span className="title-line block">{strings.aboutUsTitle}</span>
                <span className="title-line block highlight">{strings.aboutUsSubtitle}</span>
              </h1>
              <p className="text-xl leading-relaxed text-shadow-md">
                {strings.aboutUsDescription}
              </p>
            </div>
          </div>
          <div className="hero-right flex-1 flex justify-center items-center">
            <div className="building-animation relative w-full h-96 flex justify-center items-center">
              <div className="building-complex relative flex gap-4 items-end">
                <div className="building-tower tower-1 relative w-15 bg-gradient-to-t from-slate-600 to-slate-500 rounded-t-lg shadow-2xl">
                  <div className="building-windows absolute top-2.5 left-2 right-2 grid grid-cols-2 gap-1">
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                  </div>
                </div>
                <div className="building-tower tower-2 relative w-15 bg-gradient-to-t from-slate-600 to-slate-500 rounded-t-lg shadow-2xl">
                  <div className="building-windows absolute top-2.5 left-2 right-2 grid grid-cols-2 gap-1">
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                  </div>
                </div>
                <div className="building-tower tower-3 relative w-15 bg-gradient-to-t from-slate-600 to-slate-500 rounded-t-lg shadow-2xl">
                  <div className="building-windows absolute top-2.5 left-2 right-2 grid grid-cols-2 gap-1">
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                    <div className="window h-5 bg-gradient-to-br from-yellow-400 to-yellow-500 rounded-sm"></div>
                  </div>
                </div>
              </div>
              <div className="floating-elements absolute inset-0 w-full h-full">
                <div className="floating-icon icon-1 absolute text-2xl">🏢</div>
                <div className="floating-icon icon-2 absolute text-2xl">🤖</div>
                <div className="floating-icon icon-3 absolute text-2xl">📊</div>
                <div className="floating-icon icon-4 absolute text-2xl">☁️</div>
                <div className="floating-icon icon-5 absolute text-2xl">🔒</div>
                <div className="floating-icon icon-6 absolute text-2xl">⚡</div>
              </div>
              <div className="connection-lines absolute inset-0 w-full h-full">
                <div className="line line-1 absolute bg-gradient-to-r from-transparent via-blue-500 to-transparent h-0.5"></div>
                <div className="line line-2 absolute bg-gradient-to-r from-transparent via-blue-500 to-transparent h-0.5"></div>
                <div className="line line-3 absolute bg-gradient-to-r from-transparent via-blue-500 to-transparent h-0.5"></div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Main Content Section - Mimicking the structure from the provided image */}
      <section className="about-main-content py-20 bg-slate-800 relative">
        <div className="max-w-4xl mx-auto px-8 relative z-20">
          <h2 className="section-title text-center text-4xl font-bold mb-8 text-orange-500">{strings.platformOverviewTitle}</h2>
          <p className="text-lg leading-relaxed max-w-4xl mx-auto mb-6 text-slate-200 text-shadow-sm">
            {strings.platformOverviewParagraph1}
          </p>
          <p className="text-lg leading-relaxed max-w-4xl mx-auto mb-8 text-slate-200 text-shadow-sm">
            {strings.platformOverviewParagraph2}
          </p>

          <h3 className="text-3xl font-semibold mt-12 mb-6 text-blue-400 text-shadow-sm">{strings.whatWeOfferTitle}</h3>
          <ul className="space-y-4 max-w-4xl mx-auto">
            <li className="bg-gradient-to-r from-slate-700 to-slate-600 p-6 rounded-xl shadow-lg transition-all duration-300 border border-blue-500 border-opacity-20 relative overflow-hidden">
              <strong className="text-orange-500 text-lg text-shadow-sm">{strings.offerItem1Title}</strong> <span className="text-slate-200">{strings.offerItem1Desc}</span>
            </li>
            <li className="bg-gradient-to-r from-slate-700 to-slate-600 p-6 rounded-xl shadow-lg transition-all duration-300 border border-blue-500 border-opacity-20 relative overflow-hidden">
              <strong className="text-orange-500 text-lg text-shadow-sm">{strings.offerItem2Title}</strong> <span className="text-slate-200">{strings.offerItem2Desc}</span>
            </li>
            <li className="bg-gradient-to-r from-slate-700 to-slate-600 p-6 rounded-xl shadow-lg transition-all duration-300 border border-blue-500 border-opacity-20 relative overflow-hidden">
              <strong className="text-orange-500 text-lg text-shadow-sm">{strings.offerItem3Title}</strong> <span className="text-slate-200">{strings.offerItem3Desc}</span>
            </li>
            <li className="bg-gradient-to-r from-slate-700 to-slate-600 p-6 rounded-xl shadow-lg transition-all duration-300 border border-blue-500 border-opacity-20 relative overflow-hidden">
              <strong className="text-orange-500 text-lg text-shadow-sm">{strings.offerItem4Title}</strong> <span className="text-slate-200">{strings.offerItem4Desc}</span>
            </li>
          </ul>
        </div>
      </section>

      {/* Team Section */}
      <section className="team-section py-20 bg-gradient-to-br from-slate-900 to-slate-800 relative overflow-hidden">
        <div className="max-w-6xl mx-auto px-8">
          <div className="text-center mb-16 relative z-20">
            <h2 className="text-sm font-semibold text-orange-500 uppercase tracking-wider mb-2">{strings.teamTitle}</h2>
            <h3 className="text-4xl font-bold text-white text-shadow-lg">{strings.teamSubtitle}</h3>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-8 relative z-20">
            <div className="team-card bg-gradient-to-br from-slate-700 to-slate-600 rounded-2xl p-8 text-center shadow-2xl transition-all duration-300 relative overflow-hidden">
              <div className="team-avatar w-20 h-20 bg-gradient-to-br from-blue-500 to-blue-700 rounded-full flex items-center justify-center mx-auto mb-6 shadow-lg relative">
                <span className="avatar-letter text-3xl font-bold text-white text-shadow-sm">N</span>
              </div>
              <h4 className="team-name text-lg font-semibold text-white mb-2 text-shadow-sm">{strings.teamMember1Name}</h4>
              <p className="team-id text-slate-400 text-sm font-medium">{strings.teamMember1Id}</p>
            </div>
            <div className="team-card bg-gradient-to-br from-slate-700 to-slate-600 rounded-2xl p-8 text-center shadow-2xl transition-all duration-300 relative overflow-hidden">
              <div className="team-avatar w-20 h-20 bg-gradient-to-br from-blue-500 to-blue-700 rounded-full flex items-center justify-center mx-auto mb-6 shadow-lg relative">
                <span className="avatar-letter text-3xl font-bold text-white text-shadow-sm">D</span>
              </div>
              <h4 className="team-name text-lg font-semibold text-white mb-2 text-shadow-sm">{strings.teamMember2Name}</h4>
              <p className="team-id text-slate-400 text-sm font-medium">{strings.teamMember2Id}</p>
            </div>
            <div className="team-card bg-gradient-to-br from-slate-700 to-slate-600 rounded-2xl p-8 text-center shadow-2xl transition-all duration-300 relative overflow-hidden">
              <div className="team-avatar w-20 h-20 bg-gradient-to-br from-blue-500 to-blue-700 rounded-full flex items-center justify-center mx-auto mb-6 shadow-lg relative">
                <span className="avatar-letter text-3xl font-bold text-white text-shadow-sm">T</span>
              </div>
              <h4 className="team-name text-lg font-semibold text-white mb-2 text-shadow-sm">{strings.teamMember3Name}</h4>
              <p className="team-id text-slate-400 text-sm font-medium">{strings.teamMember3Id}</p>
            </div>
            <div className="team-card bg-gradient-to-br from-slate-700 to-slate-600 rounded-2xl p-8 text-center shadow-2xl transition-all duration-300 relative overflow-hidden">
              <div className="team-avatar w-20 h-20 bg-gradient-to-br from-blue-500 to-blue-700 rounded-full flex items-center justify-center mx-auto mb-6 shadow-lg relative">
                <span className="avatar-letter text-3xl font-bold text-white text-shadow-sm">T</span>
              </div>
              <h4 className="team-name text-lg font-semibold text-white mb-2 text-shadow-sm">{strings.teamMember4Name}</h4>
              <p className="team-id text-slate-400 text-sm font-medium">{strings.teamMember4Id}</p>
            </div>
            <div className="team-card bg-gradient-to-br from-slate-700 to-slate-600 rounded-2xl p-8 text-center shadow-2xl transition-all duration-300 relative overflow-hidden">
              <div className="team-avatar w-20 h-20 bg-gradient-to-br from-blue-500 to-blue-700 rounded-full flex items-center justify-center mx-auto mb-6 shadow-lg relative">
                <span className="avatar-letter text-3xl font-bold text-white text-shadow-sm">A</span>
              </div>
              <h4 className="team-name text-lg font-semibold text-white mb-2 text-shadow-sm">{strings.teamMember5Name}</h4>
              <p className="team-id text-slate-400 text-sm font-medium">{strings.teamMember5Id}</p>
            </div>
          </div>
        </div>
      </section>

      {/* Footer - Reusing Homepage's footer */}
      <footer className="bg-gradient-to-br from-slate-900 via-slate-800 to-slate-700 pt-12 relative overflow-hidden">
        <div className="max-w-6xl mx-auto px-8 relative z-20">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8 mb-8">
            {/* Company Info */}
            <div className="footer-section">
              <div className="footer-logo flex items-start gap-4 mb-4">
                <div className="logo-icon text-3xl">🏢</div>
                <div className="logo-text">
                  <h3 className="text-slate-100 text-xl font-bold mb-2">{strings.companyName}</h3>
                  <p className="text-slate-400 text-sm leading-relaxed">{strings.companyDescription}</p>
                </div>
              </div>
            </div>

            {/* Contact Info */}
            <div className="footer-section">
              <h4 className="text-slate-100 text-lg font-semibold mb-4 relative">{strings.contactUs}</h4>
              <div className="flex flex-col gap-4">
                <div className="contact-item flex items-start gap-3 text-slate-300 text-sm">
                  <div className="contact-icon text-lg flex-shrink-0">📍</div>
                  <span className="flex-1">{strings.address}</span>
                </div>
                <div className="contact-item flex items-start gap-3 text-slate-300 text-sm">
                  <div className="contact-icon text-lg flex-shrink-0">📱</div>
                  <span className="flex-1">{strings.phone}</span>
                </div>
                <div className="contact-item flex items-start gap-3 text-slate-300 text-sm">
                  <div className="contact-icon text-lg flex-shrink-0">📧</div>
                  <span className="flex-1">{strings.email}</span>
                </div>
                <div className="contact-item flex items-start gap-3 text-slate-300 text-sm">
                  <div className="contact-icon text-lg flex-shrink-0">🕒</div>
                  <div className="flex-1">
                    <div>{strings.businessHours}</div>
                    <div>{strings.weekendHours}</div>
                  </div>
                </div>
              </div>
            </div>

            {/* Quick Links */}
            <div className="footer-section">
              <h4 className="text-slate-100 text-lg font-semibold mb-4 relative">{strings.quickLinks}</h4>
              <ul className="space-y-2">
                <li><button className="footer-link text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1" onClick={() => navigate('/about')}>{strings.aboutUs}</button></li>
                <li><button className="footer-link text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1" onClick={() => navigate('/services')}>{strings.services}</button></li>
                <li><button className="footer-link text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1" onClick={() => navigate('/support')}>{strings.support}</button></li>
                <li><button className="footer-link text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1" onClick={() => navigate('/privacy')}>{strings.privacy}</button></li>
                <li><button className="footer-link text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1" onClick={() => navigate('/terms')}>{strings.terms}</button></li>
              </ul>
            </div>

            {/* Social Media */}
            <div className="footer-section">
              <h4 className="text-slate-100 text-lg font-semibold mb-4 relative">{strings.followUs}</h4>
              <div className="flex flex-col gap-3">
                <button className="social-link flex items-center gap-3 text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1">
                  <i className="fab fa-facebook text-lg w-5 text-center"></i>
                  Facebook
                </button>
                <button className="social-link flex items-center gap-3 text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1">
                  <i className="fab fa-youtube text-lg w-5 text-center"></i>
                  YouTube
                </button>
                <button className="social-link flex items-center gap-3 text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1">
                  <i className="fab fa-linkedin text-lg w-5 text-center"></i>
                  LinkedIn
                </button>
                <button className="social-link flex items-center gap-3 text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1">
                  <i className="fab fa-twitter text-lg w-5 text-center"></i>
                  Twitter
                </button>
              </div>
            </div>
          </div>

          {/* Footer Bottom */}
          <div className="border-t border-blue-500 border-opacity-20 pt-6 mt-8">
            <div className="flex justify-between items-center flex-wrap gap-4">
              <p className="text-slate-500 text-sm">{strings.allRightsReserved}</p>
              <div className="flex gap-6">
                <button className="footer-link text-slate-500 text-sm transition-all duration-300 hover:text-blue-400">{strings.privacy}</button>
                <button className="footer-link text-slate-500 text-sm transition-all duration-300 hover:text-blue-400">{strings.terms}</button>
              </div>
            </div>
          </div>
        </div>
      </footer>

      {/* Scroll to Top Button */}
      <ScrollToTop />
    </div>
  );
};

export default AboutUs;
