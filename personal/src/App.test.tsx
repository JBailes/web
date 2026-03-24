import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import App from './App'

describe('App', () => {
  it('renders name heading', () => {
    render(<App />)
    expect(screen.getByRole('heading', { level: 1, name: 'Jared Bailes' })).toBeInTheDocument()
  })

  it('renders avatar initials', () => {
    render(<App />)
    const avatar = document.querySelector('.avatar')
    expect(avatar).toBeInTheDocument()
    expect(avatar?.textContent).toBe('JB')
  })

  it('renders GitHub link with correct href', () => {
    render(<App />)
    const link = screen.getByRole('link', { name: /github/i })
    expect(link).toHaveAttribute('href', 'https://github.com/JBailes')
  })

  it('renders LinkedIn link with correct href', () => {
    render(<App />)
    const link = screen.getByRole('link', { name: /linkedin/i })
    expect(link).toHaveAttribute('href', 'https://www.linkedin.com/in/jaredbailes/')
  })

  it('all external links open in new tab with noopener', () => {
    render(<App />)
    const links = screen.getAllByRole('link')
    for (const link of links) {
      expect(link).toHaveAttribute('target', '_blank')
      expect(link).toHaveAttribute('rel', 'noopener noreferrer')
    }
  })

  it('glow element is aria-hidden', () => {
    render(<App />)
    const glow = document.querySelector('.glow')
    expect(glow).toHaveAttribute('aria-hidden', 'true')
  })

  it('avatar is aria-hidden', () => {
    render(<App />)
    const avatar = document.querySelector('.avatar')
    expect(avatar).toHaveAttribute('aria-hidden', 'true')
  })
})
