import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import NavigationBar from '../../components/NavigationBar';
import ScrollToTop from '../../components/ScrollToTop';
import ChatbotWidget from '../../components/ChatbotWidget';
import { useLanguage } from '../../hooks/useLanguage';
import '../../styles/HomepageTailwind.css';

const Homepage = () => {
  const navigate = useNavigate();
  const [isLoaded, setIsLoaded] = useState(false);
  const { strings } = useLanguage();

  useEffect(() => {
    setIsLoaded(true);
  }, []);


  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-700 text-white overflow-x-hidden relative">
      {/* Background Animation */}
      <div className="background-animation fixed inset-0 z-10 pointer-events-none">
        <div className="grid-lines"></div>
        <div className="floating-particles absolute inset-0 w-full h-full">
          {[...Array(50)].map((_, i) => (
            <div key={i} className="particle absolute w-1 h-1 bg-blue-400 rounded-full" style={{
              left: `${Math.random() * 100}%`,
              animationDelay: `${Math.random() * 3}s`,
              animationDuration: `${3 + Math.random() * 4}s`
            }}></div>
          ))}
        </div>
        <div className="data-flow absolute inset-0 w-full h-full">
          {[...Array(8)].map((_, i) => (
            <div key={i} className="flow-line absolute w-0.5 h-24 bg-gradient-to-b from-transparent via-blue-500 to-transparent" style={{
              left: `${i * 12.5}%`,
              animationDelay: `${i * 0.5}s`
            }}></div>
          ))}
        </div>
      </div>

      {/* Navigation Bar */}
      <NavigationBar activePage="home" />

      {/* Hero Section */}
      <section className="relative min-h-screen flex items-center pt-20 px-8 z-20">
        <div className="max-w-6xl mx-auto grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
          <div className={`hero-text ${isLoaded ? 'animate-in' : ''}`}>
            
            <h1 className="text-4xl lg:text-5xl font-extrabold leading-tight mb-6">
              <span className="title-line block">{strings.homepageTitle}</span>
              <span className="title-line block highlight">{strings.homepageSubtitle}</span>
            </h1>
            
            <p className="text-xl text-slate-300 mb-8 leading-relaxed">
              {strings.homepageDescription}
            </p>

            <div className="flex gap-8 mb-8">
              <div className="text-center">
                <div className="stat-number text-3xl font-extrabold text-blue-600 mb-1">500+</div>
                <div className="stat-label text-lg font-semibold text-blue-600">{strings.buildings}</div>
              </div>
              <div className="text-center">
                <div className="stat-number text-3xl font-extrabold text-blue-600 mb-1">50K+</div>
                <div className="stat-label text-lg font-semibold text-blue-600">{strings.residents}</div>
              </div>
              <div className="text-center">
                <div className="stat-number text-3xl font-extrabold text-blue-600 mb-1">99.9%</div>
                <div className="stat-label text-lg font-semibold text-blue-600">{strings.uptime}</div>
              </div>
            </div>

            <div className="flex gap-4">
              <button className="cta-btn primary px-8 py-4 rounded-lg font-semibold transition-all duration-300 flex items-center gap-2 text-lg" onClick={() => navigate('/login')}>
                <i className="fas fa-rocket"></i>
                {strings.experienceNow}
              </button>
              <button className="cta-btn secondary px-8 py-4 rounded-lg font-semibold transition-all duration-300 flex items-center gap-2 text-lg">
                <i className="fas fa-play"></i>
                {strings.learnMore}
              </button>
            </div>
          </div>

          <div className={`hero-visual ${isLoaded ? 'animate-in' : ''}`}>
            <div className="building-showcase relative h-96 flex items-end justify-center gap-8">
              <div className="building building-1 relative bg-gradient-to-t from-slate-700 to-slate-600 rounded-t-lg shadow-2xl">
                <div className="building-windows absolute top-5 left-2.5 right-2.5 bottom-5 grid grid-cols-4 gap-1">
                  {[...Array(20)].map((_, i) => (
                    <div key={i} className={`window rounded-sm ${i % 3 === 0 ? 'active' : ''}`}></div>
                  ))}
                </div>
                <div className="building-roof absolute -top-2.5 -left-1 -right-1 h-5 bg-gradient-to-r from-red-600 to-red-700 rounded-t-lg"></div>
              </div>
              
              <div className="building building-2 relative bg-gradient-to-t from-slate-700 to-slate-600 rounded-t-lg shadow-2xl">
                <div className="building-windows absolute top-5 left-2.5 right-2.5 bottom-5 grid grid-cols-4 gap-1">
                  {[...Array(15)].map((_, i) => (
                    <div key={i} className={`window rounded-sm ${i % 4 === 0 ? 'active' : ''}`}></div>
                  ))}
                </div>
                <div className="building-roof absolute -top-2.5 -left-1 -right-1 h-5 bg-gradient-to-r from-red-600 to-red-700 rounded-t-lg"></div>
              </div>
              
              <div className="building building-3 relative bg-gradient-to-t from-slate-700 to-slate-600 rounded-t-lg shadow-2xl">
                <div className="building-windows absolute top-5 left-2.5 right-2.5 bottom-5 grid grid-cols-4 gap-1">
                  {[...Array(25)].map((_, i) => (
                    <div key={i} className={`window rounded-sm ${i % 2 === 0 ? 'active' : ''}`}></div>
                  ))}
                </div>
                <div className="building-roof absolute -top-2.5 -left-1 -right-1 h-5 bg-gradient-to-r from-red-600 to-red-700 rounded-t-lg"></div>
              </div>

              <div className="floating-elements absolute inset-0">
                <div className="floating-icon absolute text-3xl">📊</div>
                <div className="floating-icon absolute text-3xl">🔧</div>
                <div className="floating-icon absolute text-3xl">👥</div>
                <div className="floating-icon absolute text-3xl">💰</div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-24 px-8 bg-slate-900 bg-opacity-80 relative z-20">
        <div className="max-w-6xl mx-auto">
          <h2 className="section-title text-center text-4xl font-bold mb-12">{strings.featuredFeatures}</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            <div className="feature-card bg-slate-800 bg-opacity-60 p-8 rounded-xl text-center transition-all duration-300 border border-blue-500 border-opacity-20">
              <div className="feature-icon text-5xl mb-4">🏠</div>
              <h3 className="text-xl mb-4 text-slate-200">{strings.apartmentManagement}</h3>
              <p className="text-slate-400 leading-relaxed">{strings.apartmentManagementDesc}</p>
            </div>
            <div className="feature-card bg-slate-800 bg-opacity-60 p-8 rounded-xl text-center transition-all duration-300 border border-blue-500 border-opacity-20">
              <div className="feature-icon text-5xl mb-4">👥</div>
              <h3 className="text-xl mb-4 text-slate-200">{strings.residentManagement}</h3>
              <p className="text-slate-400 leading-relaxed">{strings.residentManagementDesc}</p>
            </div>
            <div className="feature-card bg-slate-800 bg-opacity-60 p-8 rounded-xl text-center transition-all duration-300 border border-blue-500 border-opacity-20">
              <div className="feature-icon text-5xl mb-4">🤖</div>
              <h3 className="text-xl mb-4 text-slate-200">{strings.smartServices}</h3>
              <p className="text-slate-400 leading-relaxed">{strings.smartServicesDesc}</p>
            </div>
            <div className="feature-card bg-slate-800 bg-opacity-60 p-8 rounded-xl text-center transition-all duration-300 border border-blue-500 border-opacity-20">
              <div className="feature-icon text-5xl mb-4">🔐</div>
              <h3 className="text-xl mb-4 text-slate-200">{strings.securitySystem}</h3>
              <p className="text-slate-400 leading-relaxed">{strings.securitySystemDesc}</p>
            </div>
            <div className="feature-card bg-slate-800 bg-opacity-60 p-8 rounded-xl text-center transition-all duration-300 border border-blue-500 border-opacity-20">
              <div className="feature-icon text-5xl mb-4">💰</div>
              <h3 className="text-xl mb-4 text-slate-200">{strings.financialManagement}</h3>
              <p className="text-slate-400 leading-relaxed">{strings.financialManagementDesc}</p>
            </div>
            <div className="feature-card bg-slate-800 bg-opacity-60 p-8 rounded-xl text-center transition-all duration-300 border border-blue-500 border-opacity-20">
              <div className="feature-icon text-5xl mb-4">🔧</div>
              <h3 className="text-xl mb-4 text-slate-200">{strings.maintenanceSystem}</h3>
              <p className="text-slate-400 leading-relaxed">{strings.maintenanceSystemDesc}</p>
            </div>
          </div>
        </div>
      </section>

      {/* About Section */}
      <section className="about-section py-20 relative overflow-hidden">
        <div className="max-w-6xl mx-auto px-8">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center relative z-20">
            <div className="about-text text-slate-200">
              <h2 className="section-title text-4xl font-bold mb-6">{strings.aboutTitle}</h2>
              <h3 className="about-subtitle text-blue-400 text-2xl font-semibold mb-6">{strings.aboutSubtitle}</h3>
              <p className="about-description text-lg leading-relaxed mb-8 text-slate-300">{strings.aboutDescription}</p>
              
              <div className="grid gap-6">
                <div className="highlight-item">
                  <h4 className="text-slate-100 text-xl font-semibold mb-2">{strings.ourMission}</h4>
                  <p className="text-slate-400 leading-relaxed">{strings.ourMissionDesc}</p>
                </div>
                <div className="highlight-item">
                  <h4 className="text-slate-100 text-xl font-semibold mb-2">{strings.ourVision}</h4>
                  <p className="text-slate-400 leading-relaxed">{strings.ourVisionDesc}</p>
                </div>
              </div>
            </div>
            
            <div className="about-visual flex justify-center items-center">
              <div className="about-image">
                <div className="building-illustration relative w-80 h-80 flex justify-center items-center">
                  <div className="building-tower text-8xl">🏢</div>
                  <div className="ai-elements absolute inset-0 w-full h-full">
                    <div className="ai-icon absolute text-3xl top-1/5 right-1/5">🤖</div>
                    <div className="data-flow absolute text-3xl bottom-3/10 left-1/10">📊</div>
                    <div className="cloud-icon absolute text-3xl top-3/5 right-1/10">☁️</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>


      {/* Why Choose Us Section */}
      <section className="py-20 bg-gradient-to-br from-slate-800 to-slate-700">
        <div className="max-w-6xl mx-auto px-8">
          <h2 className="section-title text-center text-4xl font-bold mb-12">{strings.whyChooseUs}</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8 mt-12">
            <div className="why-choose-card text-center p-10 bg-slate-800 bg-opacity-80 rounded-2xl border border-slate-600 border-opacity-50 transition-all duration-300 backdrop-blur-sm">
              <div className="card-icon text-5xl mb-6 block">🚀</div>
              <h3 className="text-white text-xl font-semibold mb-4">{strings.technology}</h3>
              <p className="text-white leading-relaxed">{strings.technologyDesc}</p>
            </div>
            <div className="why-choose-card text-center p-10 bg-slate-800 bg-opacity-80 rounded-2xl border border-slate-600 border-opacity-50 transition-all duration-300 backdrop-blur-sm">
              <div className="card-icon text-5xl mb-6 block">💼</div>
              <h3 className="text-white text-xl font-semibold mb-4">{strings.experience}</h3>
              <p className="text-white leading-relaxed">{strings.experienceDesc}</p>
            </div>
            <div className="why-choose-card text-center p-10 bg-slate-800 bg-opacity-80 rounded-2xl border border-slate-600 border-opacity-50 transition-all duration-300 backdrop-blur-sm">
              <div className="card-icon text-5xl mb-6 block">🛠️</div>
              <h3 className="text-white text-xl font-semibold mb-4">{strings.support24}</h3>
              <p className="text-white leading-relaxed">{strings.support24Desc}</p>
            </div>
            <div className="why-choose-card text-center p-10 bg-slate-800 bg-opacity-80 rounded-2xl border border-slate-600 border-opacity-50 transition-all duration-300 backdrop-blur-sm">
              <div className="card-icon text-5xl mb-6 block">🔒</div>
              <h3 className="text-white text-xl font-semibold mb-4">{strings.securityAbsolute}</h3>
              <p className="text-white leading-relaxed">{strings.securityAbsoluteDesc}</p>
            </div>
          </div>
        </div>
      </section>

      {/* Technology Section */}
      <section className="technology-section py-20 bg-gradient-to-br from-slate-800 to-slate-900 relative overflow-hidden">
        <div className="max-w-6xl mx-auto px-8">
          <h2 className="section-title text-center text-4xl font-bold mb-6">{strings.technologyTitle}</h2>
          <h3 className="technology-subtitle text-purple-400 text-xl font-medium text-center mb-12">{strings.technologySubtitle}</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8 mt-12 relative z-20">
            <div className="tech-card text-center p-10 bg-purple-500 bg-opacity-10 rounded-2xl border border-purple-500 border-opacity-20 transition-all duration-300 relative overflow-hidden">
              <div className="tech-icon text-5xl mb-6 block">🤖</div>
              <h3 className="text-slate-100 text-xl font-semibold mb-4">{strings.aiPowered}</h3>
              <p className="text-slate-300 leading-relaxed">{strings.aiPoweredDesc}</p>
            </div>
            <div className="tech-card text-center p-10 bg-purple-500 bg-opacity-10 rounded-2xl border border-purple-500 border-opacity-20 transition-all duration-300 relative overflow-hidden">
              <div className="tech-icon text-5xl mb-6 block">🌐</div>
              <h3 className="text-slate-100 text-xl font-semibold mb-4">{strings.iotIntegration}</h3>
              <p className="text-slate-300 leading-relaxed">{strings.iotIntegrationDesc}</p>
            </div>
            <div className="tech-card text-center p-10 bg-purple-500 bg-opacity-10 rounded-2xl border border-purple-500 border-opacity-20 transition-all duration-300 relative overflow-hidden">
              <div className="tech-icon text-5xl mb-6 block">☁️</div>
              <h3 className="text-slate-100 text-xl font-semibold mb-4">{strings.cloudBased}</h3>
              <p className="text-slate-300 leading-relaxed">{strings.cloudBasedDesc}</p>
            </div>
            <div className="tech-card text-center p-10 bg-purple-500 bg-opacity-10 rounded-2xl border border-purple-500 border-opacity-20 transition-all duration-300 relative overflow-hidden">
              <div className="tech-icon text-5xl mb-6 block">📱</div>
              <h3 className="text-slate-100 text-xl font-semibold mb-4">{strings.mobileFirst}</h3>
              <p className="text-slate-300 leading-relaxed">{strings.mobileFirstDesc}</p>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
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
                <li><button className="footer-link text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1">{strings.aboutUs}</button></li>
                <li><button className="footer-link text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1">{strings.services}</button></li>
                <li><button className="footer-link text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1">{strings.support}</button></li>
                <li><button className="footer-link text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1">{strings.privacy}</button></li>
                <li><button className="footer-link text-slate-400 text-sm transition-all duration-300 hover:text-blue-400 hover:translate-x-1">{strings.terms}</button></li>
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

      {/* Chatbot Widget */}
      <ChatbotWidget />
    </div>
  );
};

export default Homepage;
