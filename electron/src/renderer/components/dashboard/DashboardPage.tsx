import { useEffect, useState } from 'react';
import StatsBar from './StatsBar';
import CourseCard from './CourseCard';
import Modal from '../modals/Modal';
import GradeTrendsModal from '../modals/GradeTrendsModal';
import WhatIfCalculatorModal from '../modals/WhatIfCalculatorModal';
import GradeGoalsModal from '../modals/GradeGoalsModal';
import { useCourses } from '../../hooks/useCourses';
import { useAuthStore } from '../../state/auth-store';

export default function DashboardPage() {
  const { courses, isLoading, error, loadCourses, refreshCourses } = useCourses();
  const username = useAuthStore((s) => s.username);
  const [refreshing, setRefreshing] = useState(false);
  const [activeModal, setActiveModal] = useState<string | null>(null);

  useEffect(() => {
    loadCourses();
  }, [loadCourses]);

  // Keyboard shortcuts
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setActiveModal(null);
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, []);

  const handleRefresh = async () => {
    setRefreshing(true);
    await refreshCourses();
    setRefreshing(false);
  };

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold">Dashboard</h1>
          <p className="text-sm text-github-text-secondary mt-0.5">
            {username ? `Welcome back, ${username}` : 'Your courses and grades'}
          </p>
        </div>
        <button
          onClick={handleRefresh}
          disabled={refreshing}
          className="btn-ghost flex items-center gap-2 text-sm"
        >
          <svg
            className={`w-4 h-4 ${refreshing ? 'animate-spin' : ''}`}
            viewBox="0 0 20 20"
            fill="currentColor"
          >
            <path fillRule="evenodd" d="M15.312 11.424a5.5 5.5 0 01-9.201 2.466l-.312-.311h2.433a.75.75 0 000-1.5H4.598a.75.75 0 00-.75.75v3.634a.75.75 0 001.5 0v-2.033l.312.311a7 7 0 0011.712-3.138.75.75 0 00-1.449-.39zm-10.624-2.848a5.5 5.5 0 019.201-2.466l.312.311H11.77a.75.75 0 000 1.5h3.634a.75.75 0 00.75-.75V3.538a.75.75 0 00-1.5 0V5.57l-.312-.311a7 7 0 00-11.712 3.138.75.75 0 001.449.39z" clipRule="evenodd" />
          </svg>
          Refresh
        </button>
      </div>

      {/* Stats */}
      <StatsBar courses={courses} />

      {/* Quick Actions */}
      <div className="flex flex-wrap gap-2">
        <button
          onClick={() => setActiveModal('trends')}
          className="card px-4 py-2 text-sm text-github-text-secondary hover:text-github-text-primary hover:border-github-accent/50 transition-all flex items-center gap-2"
        >
          <svg className="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
            <path d="M15.5 2A1.5 1.5 0 0014 3.5v13a1.5 1.5 0 001.5 1.5h1a1.5 1.5 0 001.5-1.5v-13A1.5 1.5 0 0016.5 2h-1zM9.5 6A1.5 1.5 0 008 7.5v9A1.5 1.5 0 009.5 18h1a1.5 1.5 0 001.5-1.5v-9A1.5 1.5 0 0010.5 6h-1zM3.5 10A1.5 1.5 0 002 11.5v5A1.5 1.5 0 003.5 18h1A1.5 1.5 0 006 16.5v-5A1.5 1.5 0 004.5 10h-1z" />
          </svg>
          Grade Trends
        </button>
        <button
          onClick={() => setActiveModal('whatif')}
          className="card px-4 py-2 text-sm text-github-text-secondary hover:text-github-text-primary hover:border-github-accent/50 transition-all flex items-center gap-2"
        >
          <svg className="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 6a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 6zm0 9a1 1 0 100-2 1 1 0 000 2z" clipRule="evenodd" />
          </svg>
          What-If Calculator
        </button>
        <button
          onClick={() => setActiveModal('goals')}
          className="card px-4 py-2 text-sm text-github-text-secondary hover:text-github-text-primary hover:border-github-accent/50 transition-all flex items-center gap-2"
        >
          <svg className="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M10 1c-1.828 0-3.623.149-5.371.435a.75.75 0 00-.629.74v.756a.75.75 0 00.629.74A49.818 49.818 0 0010 4c1.828 0 3.623-.149 5.371-.435a.75.75 0 00.629-.74v-.756a.75.75 0 00-.629-.74A49.818 49.818 0 0010 1zM8.25 7a.75.75 0 01.75.75v.5a.75.75 0 01-1.5 0v-.5A.75.75 0 018.25 7zm3 .75a.75.75 0 00-1.5 0v.5a.75.75 0 001.5 0v-.5zM9 11.25a.75.75 0 01.75.75v.5a.75.75 0 01-1.5 0v-.5A.75.75 0 019 11.25zm3 .75a.75.75 0 00-1.5 0v.5a.75.75 0 001.5 0v-.5zM8.25 15a.75.75 0 01.75.75v.5a.75.75 0 01-1.5 0v-.5A.75.75 0 018.25 15zm3 .75a.75.75 0 00-1.5 0v.5a.75.75 0 001.5 0v-.5z" clipRule="evenodd" />
          </svg>
          Grade Goals
        </button>
      </div>

      {/* Course List */}
      <div>
        <h2 className="text-sm font-medium text-github-text-secondary uppercase tracking-wide mb-3">
          Your Courses
        </h2>

        {isLoading && (
          <div className="space-y-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="card p-4 animate-pulse">
                <div className="flex items-center gap-3">
                  <div className="h-5 w-24 bg-github-border rounded" />
                  <div className="h-5 w-12 bg-github-border rounded" />
                  <div className="flex-1" />
                </div>
                <div className="h-4 w-48 bg-github-border rounded mt-2" />
              </div>
            ))}
          </div>
        )}

        {!isLoading && error && (
          <div className="card p-6 text-center">
            <p className="text-github-danger text-sm">{error}</p>
          </div>
        )}

        {!isLoading && !error && courses.length === 0 && (
          <div className="card p-8 text-center">
            <p className="text-github-text-secondary">No courses found.</p>
          </div>
        )}

        {!isLoading && courses.length > 0 && (
          <div className="space-y-3">
            {courses.map((course) => (
              <CourseCard key={course.code} course={course} />
            ))}
          </div>
        )}
      </div>

      {/* Modals */}
      <Modal isOpen={activeModal === 'trends'} onClose={() => setActiveModal(null)} title="Grade Trends">
        <GradeTrendsModal courses={courses} />
      </Modal>
      <Modal isOpen={activeModal === 'whatif'} onClose={() => setActiveModal(null)} title="What-If Calculator">
        <WhatIfCalculatorModal />
      </Modal>
      <Modal isOpen={activeModal === 'goals'} onClose={() => setActiveModal(null)} title="Grade Goals">
        <GradeGoalsModal />
      </Modal>
    </div>
  );
}
