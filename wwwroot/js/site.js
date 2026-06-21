window.playAudioById = function(id) {
  try {
    const audio = document.getElementById(id);
    if (!audio) { console.warn('audio element not found', id); return; }
    // Unmute if needed and play
    audio.currentTime = 0;
    const p = audio.play();
    if (p && p.catch) p.catch(err => console.warn('audio play failed', err));
  } catch (err) {
    console.warn('playAudioById error', err);
  }
};

window.pauseAudioById = function(id) {
  try {
    const audio = document.getElementById(id);
    if (!audio) { console.warn('audio element not found', id); return; }
    audio.pause();
  } catch (err) {
    console.warn('pauseAudioById error', err);
  }
};

window.stopAudioById = function(id) {
  try {
    const audio = document.getElementById(id);
    if (!audio) { console.warn('audio element not found', id); return; }
    audio.pause();
    audio.currentTime = 0;
  } catch (err) {
    console.warn('stopAudioById error', err);
  }
};

// Custom Modal management (without Bootstrap dependency)
window.customModal = {
  show: function(modalId) {
    try {
      const modalElement = document.getElementById(modalId);
      if (!modalElement) { console.warn('modal element not found', modalId); return; }
      modalElement.style.display = 'block';
      // Add backdrop
      let backdrop = document.querySelector('.modal-backdrop');
      if (!backdrop) {
        backdrop = document.createElement('div');
        backdrop.className = 'modal-backdrop fade show';
        backdrop.style.display = 'block';
        document.body.appendChild(backdrop);
      }
      document.body.style.overflow = 'hidden';
    } catch (err) {
      console.warn('customModal.show error', err);
    }
  },
  hide: function(modalId) {
    try {
      const modalElement = document.getElementById(modalId);
      if (!modalElement) { console.warn('modal element not found', modalId); return; }
      modalElement.style.display = 'none';
      // Remove backdrop
      const backdrop = document.querySelector('.modal-backdrop');
      if (backdrop) backdrop.remove();
      document.body.style.overflow = 'auto';
    } catch (err) {
      console.warn('customModal.hide error', err);
    }
  }
};

window.scrollIntoViewById = function(elementId) {
  try {
    const element = document.getElementById(elementId);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  } catch (err) {
    console.warn('scrollIntoViewById error', err);
  }
};
