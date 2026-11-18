// Dados 3D de HackMaster Companion (F003). Contrato: specs/004-dados-3d/contracts/dice3d-js.md.
//
// El resultado NO lo decide este módulo: lo decide el tirador en C# (DiceBoard.Plan). Aquí se
// simula la física sin dibujar, se mira qué cara queda arriba en cada dado, se cambian sus números
// para que esa cara lleve el valor pedido y se reproduce la grabación en pantalla (research §2).

import * as THREE from '../lib/three.module.js';
import * as CANNON from '../lib/cannon-es.js';

const STEP = 1 / 60;
const WAVE_STEPS = 60 * 3;         // una ola espera como mucho 3 s a que se pare la anterior
const LAST_WAVE_STEPS = 60 * 5;    // la última ola, como mucho 5 s
const MAX_PLAYBACK = 3.6;          // reproducción: como mucho 3,6 s (FR-004: menos de 4 s)
const REST_STEPS = 20;             // pasos seguidos casi quietos para dar un dado por parado
const SETTLE_STEPS = 15;           // pasos para asentar un dado que acabó inclinado
const MIN_PLAYBACK = 1.5;          // la reproducción no baja de 1,5 s aunque la simulación tarde
const TABLE_WIDTH = 10;
const DIE_COLOR = '#f4efe6';
const DIE_INK = '#1f2328';
// Dados extra por penetración: llevan el aspecto del jugador y un aura de este color, que se ve en
// las dos mesas de F007 (F008, FR-007).
const AURA_COLOR = '#e8b54a';
const EFFECT_SECONDS = 0.6;        // duración de un efecto de penetración

// ---------------------------------------------------------------------------------------------
// Formas
// ---------------------------------------------------------------------------------------------

const PHI = (1 + Math.sqrt(5)) / 2;

function range(from, to) {
  return Array.from({ length: to - from + 1 }, (_, i) => String(from + i));
}

function trapezohedron() {
  // d10: dos polos y un anillo de 10 vértices a alturas ±z, con z elegida para que cada cara
  // (cometa) sea plana: H = z (1 + cos 36°) / (1 − cos 36°).
  const cos36 = Math.cos(Math.PI / 5);
  const z = (1 - cos36) / (1 + cos36);
  const vertices = [[0, 1, 0], [0, -1, 0]];
  for (let i = 0; i < 10; i++) {
    const angle = (i * Math.PI) / 5;
    vertices.push([Math.cos(angle), i % 2 === 0 ? z : -z, Math.sin(angle)]);
  }
  return vertices;
}

const SHAPES = {
  D4: { scale: 0.75, labels: range(1, 4), byVertex: true,
    vertices: [[1, 1, 1], [1, -1, -1], [-1, 1, -1], [-1, -1, 1]] },
  D6: { scale: 0.5, labels: range(1, 6),
    vertices: [-1, 1].flatMap(x => [-1, 1].flatMap(y => [-1, 1].map(z => [x, y, z]))) },
  D8: { scale: 0.8, labels: range(1, 8),
    vertices: [[1, 0, 0], [-1, 0, 0], [0, 1, 0], [0, -1, 0], [0, 0, 1], [0, 0, -1]] },
  D10: { scale: 0.75, labels: range(1, 10), vertices: trapezohedron() },
  D10Tens: { scale: 0.75, labels: ['00', '10', '20', '30', '40', '50', '60', '70', '80', '90'], vertices: trapezohedron() },
  D12: { scale: 0.48, labels: range(1, 12),
    vertices: [
      ...[-1, 1].flatMap(x => [-1, 1].flatMap(y => [-1, 1].map(z => [x, y, z]))),
      ...[-1, 1].flatMap(a => [-1, 1].flatMap(b => [[0, a / PHI, b * PHI], [a / PHI, b * PHI, 0], [b * PHI, 0, a / PHI]])),
    ] },
  D20: { scale: 0.48, labels: range(1, 20),
    vertices: [-1, 1].flatMap(a => [-1, 1].flatMap(b => [[0, a, b * PHI], [a, b * PHI, 0], [b * PHI, 0, a]])) },
};

// Etiquetas del d10 de unidades de un d100 (va justo detrás de su d10 de decenas en el plan).
const UNITS_LABELS = range(0, 9);

const geometryCache = new Map();

/** Caras de un poliedro convexo a partir de sus vértices: planos que dejan todos los vértices a un lado. */
function hull(points) {
  const v = points.map(p => new THREE.Vector3(...p));
  const faces = [];
  const seen = new Set();
  const eps = 1e-6;
  for (let i = 0; i < v.length; i++) {
    for (let j = i + 1; j < v.length; j++) {
      for (let k = j + 1; k < v.length; k++) {
        const normal = new THREE.Vector3().subVectors(v[j], v[i]).cross(new THREE.Vector3().subVectors(v[k], v[i]));
        if (normal.lengthSq() < 1e-12) continue;
        normal.normalize();
        let d = normal.dot(v[i]);
        let above = 0, below = 0;
        for (const p of v) {
          const s = normal.dot(p) - d;
          if (s > eps) above++; else if (s < -eps) below++;
        }
        if (above && below) continue;
        if (above) { normal.negate(); d = -d; }
        const on = v.map((p, index) => [p, index]).filter(([p]) => Math.abs(normal.dot(p) - d) < eps).map(([, index]) => index);
        const key = [...on].sort((a, b) => a - b).join(',');
        if (seen.has(key)) continue;
        seen.add(key);
        const center = on.reduce((sum, index) => sum.add(v[index]), new THREE.Vector3()).divideScalar(on.length);
        const u = new THREE.Vector3().subVectors(v[on[0]], center).normalize();
        const w = new THREE.Vector3().crossVectors(normal, u);
        on.sort((a, b) => {
          const pa = new THREE.Vector3().subVectors(v[a], center);
          const pb = new THREE.Vector3().subVectors(v[b], center);
          return Math.atan2(pa.dot(w), pa.dot(u)) - Math.atan2(pb.dot(w), pb.dot(u));
        });
        faces.push({ indices: on, normal, center, u, w });
      }
    }
  }
  return { vertices: v, faces };
}

/** Forma lista para usar: vértices escalados, caras ordenadas y figura de física. */
function shapeData(name) {
  if (geometryCache.has(name)) return geometryCache.get(name);
  const shape = SHAPES[name];
  const scaled = shape.vertices.map(p => p.map(c => c * shape.scale));
  const { vertices, faces } = hull(scaled);
  const physics = new CANNON.ConvexPolyhedron({
    vertices: vertices.map(p => new CANNON.Vec3(p.x, p.y, p.z)),
    faces: faces.map(face => face.indices),
  });
  const data = { name, shape, vertices, faces, physics };
  geometryCache.set(name, data);
  return data;
}

// ---------------------------------------------------------------------------------------------
// Etiquetas: qué número lleva cada cara (o cada vértice, en el d4)
// ---------------------------------------------------------------------------------------------

function baseLabels(item, previous) {
  if (item.shape === 'D10' && previous && previous.shape === 'D10Tens' && previous.wave === item.wave) {
    return [...UNITS_LABELS];
  }
  return [...SHAPES[item.shape].labels];
}

/** Índice de la cara (o vértice del d4) que queda arriba con esa orientación. */
function topSlot(data, quaternion) {
  const q = new THREE.Quaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
  let best = -Infinity;
  let slot = 0;
  if (data.shape.byVertex) {
    data.vertices.forEach((vertex, index) => {
      const y = vertex.clone().applyQuaternion(q).y;
      if (y > best) { best = y; slot = index; }
    });
  } else {
    data.faces.forEach((face, index) => {
      const y = face.normal.clone().applyQuaternion(q).y;
      if (y > best) { best = y; slot = index; }
    });
  }
  return slot;
}

/** Pone la cara pedida en el hueco de arriba, intercambiándola con donde estuviera. */
function forceLabel(labels, slot, wanted) {
  const result = [...labels];
  const current = result.indexOf(wanted);
  if (current >= 0) {
    [result[slot], result[current]] = [result[current], result[slot]];
  } else {
    result[slot] = wanted;
  }
  return result;
}

// ---------------------------------------------------------------------------------------------
// Física
// ---------------------------------------------------------------------------------------------

/** Generador con semilla (mulberry32): solo decide cómo ruedan los dados, nunca el resultado. */
function seeded(seed) {
  let t = seed >>> 0;
  return () => {
    t = (t + 0x6D2B79F5) >>> 0;
    let r = Math.imul(t ^ (t >>> 15), 1 | t);
    r = (r + Math.imul(r ^ (r >>> 7), 61 | r)) ^ r;
    return ((r ^ (r >>> 14)) >>> 0) / 4294967296;
  };
}

function createWorld(depth) {
  const world = new CANNON.World({ gravity: new CANNON.Vec3(0, -30, 0) });
  world.allowSleep = true;
  world.defaultContactMaterial.friction = 0.3;
  world.defaultContactMaterial.restitution = 0.35;
  const wall = (position, axis, angle) => {
    const body = new CANNON.Body({ mass: 0, shape: new CANNON.Plane() });
    body.position.set(...position);
    if (axis) body.quaternion.setFromAxisAngle(new CANNON.Vec3(...axis), angle);
    world.addBody(body);
  };
  wall([0, 0, 0], [1, 0, 0], -Math.PI / 2);                 // suelo
  wall([-TABLE_WIDTH / 2, 0, 0], [0, 1, 0], Math.PI / 2);    // izquierda
  wall([TABLE_WIDTH / 2, 0, 0], [0, 1, 0], -Math.PI / 2);    // derecha
  wall([0, 0, -depth / 2], null, 0);                         // fondo
  wall([0, 0, depth / 2], [0, 1, 0], Math.PI);               // frente
  wall([0, 12, 0], [1, 0, 0], Math.PI / 2);                  // techo
  return world;
}

function throwDie(body, random, depth) {
  body.position.set((random() - 0.5) * (TABLE_WIDTH - 3), 3 + random() * 2, depth / 2 - 1.2 - random());
  body.quaternion.setFromEuler(random() * 6.28, random() * 6.28, random() * 6.28);
  body.velocity.set((random() - 0.5) * 6, 0, -(7 + random() * 5));
  body.angularVelocity.set((random() - 0.5) * 30, (random() - 0.5) * 30, (random() - 0.5) * 30);
  body.wakeUp();
}

function atRest(body) {
  return body.sleepState === CANNON.Body.SLEEPING
    || (body.velocity.lengthSquared() < 0.01 && body.angularVelocity.lengthSquared() < 0.01);
}

/**
 * Simula la tirada sin dibujar y graba posición y giro de cada dado en cada paso. Los dados de
 * cada ola salen cuando se para la anterior. Devuelve la grabación y las etiquetas forzadas.
 */
function run(plan, seed, depth) {
  const random = seeded(seed);
  const world = createWorld(depth);
  // Choques grabados para el sonido (F008, research §3): paso y fuerza del golpe.
  const collisions = [];
  const clock = { step: 0 };
  const items = plan.dice.map((item, index) => {
    const data = shapeData(item.shape);
    const body = new CANNON.Body({ mass: 1, shape: data.physics, sleepSpeedLimit: 0.15, sleepTimeLimit: 0.4 });
    body.linearDamping = 0.1;
    body.angularDamping = 0.1;
    body.addEventListener('collide', event => {
      const strength = Math.abs(event.contact.getImpactVelocityAlongNormal());
      if (strength > 0.8) collisions.push({ step: clock.step, strength });
    });
    return { item, data, body, labels: baseLabels(item, plan.dice[index - 1]), start: -1, frames: [] };
  });

  const waves = [...new Set(items.map(entry => entry.item.wave))].sort((a, b) => a - b);
  const maxSteps = WAVE_STEPS * (waves.length - 1) + LAST_WAVE_STEPS;
  let waveIndex = -1;
  let quietSteps = 0;
  let waveStart = 0;
  let step = 0;
  // firstFrame: índice del primer paso que se graba para los dados de la ola.
  const launch = (firstFrame) => {
    waveIndex++;
    for (const entry of items.filter(e => e.item.wave === waves[waveIndex])) {
      throwDie(entry.body, random, depth);
      world.addBody(entry.body);
      entry.start = firstFrame;
    }
    quietSteps = 0;
    waveStart = firstFrame;
  };
  if (items.length > 0) launch(0);

  // Cada ola sale cuando la anterior se para o, como mucho, a los WAVE_STEPS pasos; así todas las
  // olas salen siempre y la simulación termina aunque algún dado siga moviéndose un poco.
  for (; step < maxSteps && items.length > 0; step++) {
    clock.step = step;
    world.step(STEP);
    for (const entry of items) {
      if (entry.start >= 0) {
        const { position: p, quaternion: q } = entry.body;
        entry.frames.push([p.x, p.y, p.z, q.x, q.y, q.z, q.w]);
      }
    }
    const moving = items.filter(entry => entry.start >= 0 && !atRest(entry.body));
    quietSteps = moving.length === 0 ? quietSteps + 1 : 0;
    const lastWave = waveIndex + 1 >= waves.length;
    if (quietSteps >= REST_STEPS && lastWave) { step++; break; }
    if (!lastWave && (quietSteps >= REST_STEPS || step - waveStart >= WAVE_STEPS)) launch(step + 1);
  }

  for (const entry of items) {
    settle(entry);
    const last = entry.frames[entry.frames.length - 1];
    const slot = topSlot(entry.data, { x: last[3], y: last[4], z: last[5], w: last[6] });
    entry.labels = forceLabel(entry.labels, slot, entry.item.face);
    entry.top = slot;
  }

  const longest = Math.max(0, ...items.map(entry => entry.start + entry.frames.length));
  return { items, steps: longest, collisions };
}

/**
 * Un dado que acaba inclinado (apoyado en otro o en una pared) se asienta en unos pasos más sobre
 * la cara que tiene arriba, para que el resultado se lea sin dudas. No cambia qué cara queda arriba.
 */
function settle(entry) {
  const last = entry.frames[entry.frames.length - 1];
  const q = new THREE.Quaternion(last[3], last[4], last[5], last[6]);
  const slot = topSlot(entry.data, q);
  // Dirección del dado que debe quedar mirando hacia arriba: la normal de la cara de arriba o, en el
  // d4, la contraria a la normal de la cara de abajo.
  const local = entry.data.shape.byVertex
    ? entry.data.faces.find(face => !face.indices.includes(slot)).normal.clone().negate()
    : entry.data.faces[slot].normal.clone();
  const world = local.clone().applyQuaternion(q);
  if (world.y > 0.995) return;

  const target = new THREE.Quaternion().setFromUnitVectors(world, new THREE.Vector3(0, 1, 0)).multiply(q);
  const rest = -Math.min(...entry.data.vertices.map(v => v.clone().applyQuaternion(target).y));
  const y = last[1] > rest + 0.15 ? last[1] : rest;
  const from = new THREE.Vector3(last[0], last[1], last[2]);
  const to = new THREE.Vector3(last[0], y, last[2]);
  for (let i = 1; i <= SETTLE_STEPS; i++) {
    const t = i / SETTLE_STEPS;
    const p = from.clone().lerp(to, t);
    const r = q.clone().slerp(target, t);
    entry.frames.push([p.x, p.y, p.z, r.x, r.y, r.z, r.w]);
  }
}

/**
 * Comprobación de una simulación, para las pruebas (SC-001): por dado, la cara de arriba, cuánto
 * apunta hacia arriba esa cara (1 = plana; en el d4, el vértice de arriba frente a los demás), si
 * hay tantas caras (o vértices) como etiquetas y si las etiquetas son distintas entre sí.
 */
export function inspect(plan, seed) {
  const started = performance.now();
  const { items, steps } = run(plan, seed, TABLE_WIDTH * 0.75);
  const ms = performance.now() - started;
  return {
    ms,
    steps,
    dice: items.map(entry => {
      const last = entry.frames[entry.frames.length - 1];
      const q = new THREE.Quaternion(last[3], last[4], last[5], last[6]);
      const slot = topSlot(entry.data, q);
      const slots = entry.data.shape.byVertex ? entry.data.vertices.length : entry.data.faces.length;
      const up = entry.data.shape.byVertex
        ? flatnessOfVertex(entry.data, q, slot)
        : entry.data.faces[slot].normal.clone().applyQuaternion(q).y;
      return {
        face: entry.labels[slot],
        up,
        slots,
        labels: entry.labels.length,
        unique: new Set(entry.labels).size === entry.labels.length,
        inside: Math.abs(last[0]) < TABLE_WIDTH / 2 && Math.abs(last[2]) < TABLE_WIDTH * 0.375 && last[1] > 0,
        // F008: la malla que se ve es redondeada y sus caras están en los planos del poliedro.
        roundedVertices: roundedGeometry(entry.data).geometry.attributes.position.count,
        planeError: planeErrorOf(entry.data),
      };
    }),
  };
}

/** En el d4: la cara de abajo (la opuesta al vértice de arriba) debe mirar hacia abajo. */
function flatnessOfVertex(data, q, vertex) {
  const opposite = data.faces.findIndex(face => !face.indices.includes(vertex));
  return -data.faces[opposite].normal.clone().applyQuaternion(q).y;
}

/**
 * Caras que quedan arriba al simular el plan, leídas de la orientación final y de las etiquetas
 * que lleva cada dado. Sin dibujar: para la prueba de SC-001.
 */
export function simulate(plan, seed) {
  const { items } = run(plan, seed, TABLE_WIDTH * 0.75);
  return items.map(entry => {
    const last = entry.frames[entry.frames.length - 1];
    return entry.labels[topSlot(entry.data, { x: last[3], y: last[4], z: last[5], w: last[6] })];
  });
}

// ---------------------------------------------------------------------------------------------
// Dibujo
// ---------------------------------------------------------------------------------------------

export function isSupported() {
  try {
    const canvas = document.createElement('canvas');
    return Boolean(window.WebGLRenderingContext && (canvas.getContext('webgl2') || canvas.getContext('webgl')));
  } catch {
    return false;
  }
}

// ---------------------------------------------------------------------------------------------
// Aristas redondeadas (F008, research §1)
// ---------------------------------------------------------------------------------------------

/** Cuánto se redondea cada forma: parte de la distancia del centro a las caras. */
const ROUNDNESS = { D4: 0.2, D6: 0.22, D8: 0.18, D10: 0.16, D10Tens: 0.16, D12: 0.14, D20: 0.14 };
const SPHERE_DETAIL = 4;

/**
 * Malla redondeada de una forma: suma de Minkowski del poliedro encogido (factor s) y una esfera de
 * radio r = h·(1 − s), donde h es la distancia del centro a las caras (igual en todas las caras de
 * cada forma). Así las caras quedan en los mismos planos que las del poliedro con el que rueda: se
 * apoya igual y la física no cambia. Para cada dirección d de una esfera muy subdividida, el punto
 * es el vértice encogido que más avanza en d, más r·d; la normal es d.
 */
function roundedGeometry(data) {
  if (data.rounded) return data.rounded;
  const roundness = ROUNDNESS[data.name];
  const first = data.faces[0];
  const h = first.normal.dot(data.vertices[first.indices[0]]);
  const s = 1 - roundness;
  const r = h * roundness;
  const shrunk = data.vertices.map(v => v.clone().multiplyScalar(s));
  const geometry = new THREE.IcosahedronGeometry(1, SPHERE_DETAIL);
  const position = geometry.attributes.position;
  const normals = new Float32Array(position.count * 3);
  const uvs = new Float32Array(position.count * 2);
  const d = new THREE.Vector3();
  for (let i = 0; i < position.count; i++) {
    d.fromBufferAttribute(position, i).normalize();
    let best = shrunk[0];
    let bestDot = -Infinity;
    for (const v of shrunk) {
      const dot = v.dot(d);
      if (dot > bestDot) { bestDot = dot; best = v; }
    }
    position.setXYZ(i, best.x + d.x * r, best.y + d.y * r, best.z + d.z * r);
    normals.set([d.x, d.y, d.z], i * 3);
    // Texturas proyectadas desde el centro: en materiales de ruido la costura no se nota.
    uvs.set([0.5 + Math.atan2(d.z, d.x) / (2 * Math.PI), 0.5 + Math.asin(Math.max(-1, Math.min(1, d.y))) / Math.PI], i * 2);
  }
  geometry.setAttribute('normal', new THREE.BufferAttribute(normals, 3));
  geometry.setAttribute('uv', new THREE.BufferAttribute(uvs, 2));
  geometry.computeBoundingSphere();
  data.rounded = { geometry, s, r, h };
  return data.rounded;
}

/** Cuánto se separan los planos de las caras de la malla redondeada de los del poliedro (pruebas). */
function planeErrorOf(data) {
  if (data.planeError !== undefined) return data.planeError;
  const { geometry, h } = roundedGeometry(data);
  const position = geometry.attributes.position;
  const p = new THREE.Vector3();
  let worst = 0;
  for (const face of data.faces) {
    let max = -Infinity;
    for (let i = 0; i < position.count; i++) max = Math.max(max, p.fromBufferAttribute(position, i).dot(face.normal));
    worst = Math.max(worst, Math.abs(max - h));
  }
  data.planeError = worst;
  return worst;
}

// ---------------------------------------------------------------------------------------------
// Materiales (F008, research §2): todos generados con código, sin descargar imágenes
// ---------------------------------------------------------------------------------------------

const MATERIALS = ['Ivory', 'Marble', 'OldGold', 'BlackIron', 'Ruby', 'Obsidian', 'Bone', 'Amethyst', 'Wood', 'Stone', 'Crystal'];
const EFFECTS = ['Explosion', 'Fire', 'Blood', 'None'];
const DEFAULT_LOOK = { material: 'Ivory', color: DIE_COLOR, ink: DIE_INK, effect: 'Explosion', sound: true, quality: 'High' };
const HEX = /^#[0-9a-fA-F]{6}$/;

/** El aspecto con lo que falte o no se entienda cambiado por lo de por defecto (contrato). */
function normalizeLook(look) {
  const value = look || {};
  return {
    material: MATERIALS.includes(value.material) ? value.material : DEFAULT_LOOK.material,
    color: HEX.test(value.color) ? value.color : DEFAULT_LOOK.color,
    ink: HEX.test(value.ink) ? value.ink : DEFAULT_LOOK.ink,
    effect: EFFECTS.includes(value.effect) ? value.effect : DEFAULT_LOOK.effect,
    sound: typeof value.sound === 'boolean' ? value.sound : DEFAULT_LOOK.sound,
    quality: value.quality === 'Low' ? 'Low' : 'High',
  };
}

/** Generador con semilla fija para las texturas: el mismo material se ve siempre igual. */
function textureRandom() {
  return seeded(20261118);
}

const textureCache = new Map();

/** Textura de lienzo cacheada por clave; draw(contexto, tamaño, azar). */
function canvasTexture(key, size, draw, color = true) {
  if (textureCache.has(key)) return textureCache.get(key);
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = size;
  draw(canvas.getContext('2d'), size, textureRandom());
  const texture = new THREE.CanvasTexture(canvas);
  if (color) texture.colorSpace = THREE.SRGBColorSpace;
  texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
  textureCache.set(key, texture);
  return texture;
}

/** Mezcla un color #rrggbb con blanco (t > 0) o negro (t < 0). */
function shade(hex, t) {
  const c = new THREE.Color(hex);
  return `#${c.lerp(new THREE.Color(t > 0 ? '#ffffff' : '#000000'), Math.abs(t)).getHexString()}`;
}

const TEXTURES = {
  Marble: color => canvasTexture(`marble${color}`, 512, (g, s, random) => {
    g.fillStyle = color; g.fillRect(0, 0, s, s);
    for (let k = 0; k < 26; k++) {
      g.strokeStyle = shade(color, 0.7); g.globalAlpha = 0.15 + random() * 0.4; g.lineWidth = 0.6 + random() * 3;
      g.beginPath(); let x = random() * s, y = 0; g.moveTo(x, y);
      while (y < s) { x += (random() - 0.5) * 50; y += 16 + random() * 28; g.lineTo(x, y); }
      g.stroke();
    }
    g.globalAlpha = 1;
  }),
  Bone: color => canvasTexture(`bone${color}`, 512, (g, s, random) => {
    g.fillStyle = color; g.fillRect(0, 0, s, s);
    for (let k = 0; k < 3000; k++) { g.fillStyle = `rgba(110,80,40,${random() * 0.07})`; g.fillRect(random() * s, random() * s, 2 + random() * 6, 1 + random() * 2); }
    g.strokeStyle = 'rgba(80,55,25,0.3)'; g.lineWidth = 0.8;
    for (let k = 0; k < 8; k++) {
      g.beginPath(); let x = random() * s, y = random() * s; g.moveTo(x, y);
      for (let j = 0; j < 6; j++) { x += (random() - 0.5) * 50; y += (random() - 0.5) * 50; g.lineTo(x, y); }
      g.stroke();
    }
  }),
  Wood: color => canvasTexture(`wood${color}`, 512, (g, s, random) => {
    g.fillStyle = color; g.fillRect(0, 0, s, s);
    for (let y = 0; y < s; y += 2) {
      const wave = Math.sin(y / 23) * 6 + Math.sin(y / 7) * 2;
      g.fillStyle = shade(color, (Math.sin((y + wave * 3) / 9) > 0.55 ? -0.28 : 0.04) + (random() - 0.5) * 0.06);
      g.globalAlpha = 0.55; g.fillRect(0, y, s, 2);
    }
    g.globalAlpha = 1;
  }),
  Stone: color => canvasTexture(`stone${color}`, 256, (g, s, random) => {
    g.fillStyle = color; g.fillRect(0, 0, s, s);
    for (let k = 0; k < 5000; k++) {
      g.fillStyle = shade(color, (random() - 0.5) * 0.5); g.globalAlpha = 0.5;
      g.fillRect(random() * s, random() * s, 1 + random() * 3, 1 + random() * 3);
    }
    g.globalAlpha = 1;
  }),
};

/** Arañazos en la rugosidad de los metales. */
function scratches() {
  return canvasTexture('scratches', 256, (g, s, random) => {
    g.fillStyle = '#808080'; g.fillRect(0, 0, s, s);
    for (let k = 0; k < 400; k++) {
      const v = random() > 0.5 ? 255 : 0;
      g.strokeStyle = `rgba(${v},${v},${v},0.12)`;
      g.beginPath(); const x = random() * s, y = random() * s; g.moveTo(x, y); g.lineTo(x + (random() - 0.5) * 40, y + (random() - 0.5) * 8); g.stroke();
    }
  }, false);
}

const GEMS = { Ruby: 1.7, Amethyst: 1.55, Crystal: 1.5 };

/**
 * Material del cuerpo de un dado. Calidad baja: color liso, sin texturas, reflejos ni transmisión
 * (FR-011). Devuelve también si lleva textura o transmisión, para las pruebas.
 */
function bodyMaterial(look) {
  const { material, color, quality } = look;
  if (quality === 'Low') {
    const gem = material in GEMS;
    return { material: new THREE.MeshLambertMaterial({ color, transparent: gem, opacity: gem ? 0.85 : 1 }), textured: false, transmission: false };
  }
  if (material in TEXTURES) {
    const map = TEXTURES[material](color);
    const roughness = { Marble: 0.25, Bone: 0.75, Wood: 0.6, Stone: 0.9 }[material];
    return { material: new THREE.MeshStandardMaterial({ map, roughness }), textured: true, transmission: false };
  }
  if (material === 'OldGold' || material === 'BlackIron') {
    return { material: new THREE.MeshStandardMaterial({ color, metalness: 1, roughness: material === 'OldGold' ? 0.32 : 0.4, roughnessMap: scratches() }), textured: true, transmission: false };
  }
  if (material in GEMS) {
    return {
      material: new THREE.MeshPhysicalMaterial({ color, roughness: 0.05, transmission: material === 'Crystal' ? 0.95 : 0.85, thickness: 1.2, ior: GEMS[material], clearcoat: 1 }),
      textured: false,
      transmission: true,
    };
  }
  if (material === 'Obsidian') {
    return { material: new THREE.MeshPhysicalMaterial({ color, roughness: 0.08, clearcoat: 1, clearcoatRoughness: 0.05 }), textured: false, transmission: false };
  }
  return { material: new THREE.MeshStandardMaterial({ color, roughness: 0.45, metalness: 0.05 }), textured: false, transmission: false };
}

/** Entorno para los reflejos: un degradado cálido generado con código (research §2). */
function environment(renderer) {
  const canvas = document.createElement('canvas');
  canvas.width = 256; canvas.height = 128;
  const g = canvas.getContext('2d');
  const gradient = g.createLinearGradient(0, 0, 0, 128);
  gradient.addColorStop(0, '#fff3d6'); gradient.addColorStop(0.45, '#c08a4a'); gradient.addColorStop(0.55, '#3a2214'); gradient.addColorStop(1, '#120a06');
  g.fillStyle = gradient; g.fillRect(0, 0, 256, 128);
  g.fillStyle = '#ffffff';
  g.beginPath(); g.ellipse(70, 30, 18, 10, 0, 0, Math.PI * 2); g.fill();
  g.beginPath(); g.ellipse(190, 40, 10, 6, 0, 0, Math.PI * 2); g.fill();
  const texture = new THREE.CanvasTexture(canvas);
  texture.mapping = THREE.EquirectangularReflectionMapping;
  texture.colorSpace = THREE.SRGBColorSpace;
  const generator = new THREE.PMREMGenerator(renderer);
  const result = generator.fromEquirectangular(texture).texture;
  texture.dispose();
  generator.dispose();
  return result;
}

/** Luces y entorno de una escena de dados. */
function light(scene, renderer) {
  scene.add(new THREE.HemisphereLight(0xfff4dd, 0x3a1a10, 0.9));
  const sun = new THREE.DirectionalLight(0xffffff, 2);
  sun.position.set(4, 12, 6);
  scene.add(sun);
  scene.environment = environment(renderer);
}

/** Textura de los números de una cara, sobre fondo transparente (o los tres del d4, cerca de cada esquina). */
function faceTexture(texts, positions, ink) {
  const size = 128;
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = size;
  const context = canvas.getContext('2d');
  context.fillStyle = ink;
  context.textAlign = 'center';
  context.textBaseline = 'middle';
  texts.forEach((text, index) => {
    const [x, y] = positions[index];
    const fontSize = texts.length > 1 ? 30 : (text.length > 1 ? 48 : 60);
    context.font = `bold ${fontSize}px system-ui, sans-serif`;
    context.fillText(text, x * size, (1 - y) * size);
    if (text === '6' || text === '9') {
      context.fillRect(x * size - fontSize * 0.25, (1 - y) * size + fontSize * 0.45, fontSize * 0.5, 4);
    }
  });
  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  return texture;
}

/**
 * Un dado: la malla redondeada (compartida por todos los dados de la forma) y, en cada cara, una
 * lámina con sus números en la parte plana de la cara (F008). El grupo se mueve como un dado.
 */
function dieMesh(data, labels, look, extra = false) {
  const { geometry: rounded, s, r, h } = roundedGeometry(data);
  const group = new THREE.Group();
  const made = bodyMaterial(look);
  const body = new THREE.Mesh(rounded, made.material);
  // La malla redondeada y las texturas del material se comparten entre dados: no se liberan con el dado.
  body.userData.shared = true;
  group.add(body);
  group.userData.look = { material: look.material, textured: made.textured, transmission: made.transmission, aura: extra };
  if (extra) {
    // Aura de los dados extra por penetración: luz aditiva detrás del dado (FR-007).
    const aura = new THREE.Sprite(new THREE.SpriteMaterial({ map: glowTexture(), color: AURA_COLOR, blending: THREE.AdditiveBlending, depthWrite: false, transparent: true, opacity: 0.9 }));
    aura.scale.setScalar(h * 5.5);
    aura.userData.shared = true;
    aura.renderOrder = -1;
    group.add(aura);
  }

  data.faces.forEach((face, faceIndex) => {
    const local = face.indices.map(index => {
      const p = new THREE.Vector3().subVectors(data.vertices[index], face.center);
      return [p.dot(face.u), p.dot(face.w)];
    });
    const radius = Math.max(...local.map(([a, b]) => Math.hypot(a, b)));
    const uv = local.map(([a, b]) => [0.5 + (a / radius) * 0.46, 0.5 + (b / radius) * 0.46]);
    // La parte plana de la cara es el polígono encogido, en el plano de la cara; la lámina va
    // apenas por encima para que no parpadee.
    const corner = index => data.vertices[index].clone().multiplyScalar(s).addScaledVector(face.normal, r + 0.003);
    const positions = [];
    const uvs = [];
    for (let i = 1; i < face.indices.length - 1; i++) {
      for (const c of [0, i, i + 1]) {
        const p = corner(face.indices[c]);
        positions.push(p.x, p.y, p.z);
        uvs.push(...uv[c]);
      }
    }
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
    geometry.setAttribute('uv', new THREE.Float32BufferAttribute(uvs, 2));
    geometry.computeVertexNormals();

    const texts = data.shape.byVertex ? face.indices.map(index => labels[index]) : [labels[faceIndex]];
    const where = data.shape.byVertex ? uv.map(([u, v]) => [0.5 + (u - 0.5) * 0.62, 0.5 + (v - 0.5) * 0.62]) : [[0.5, 0.5]];
    const material = new THREE.MeshStandardMaterial({ map: faceTexture(texts, where, look.ink), transparent: true, roughness: 0.5, depthWrite: false });
    group.add(new THREE.Mesh(geometry, material));
  });
  return group;
}

/** Libera las geometrías y materiales propios de un dado (no la malla redondeada compartida). */
function disposeDie(group) {
  group.traverse(object => {
    if (object.isSprite) { object.material.dispose(); return; }
    if (!object.isMesh) return;
    if (!object.userData.shared) {
      object.geometry.dispose();
      object.material.map?.dispose();
    }
    object.material.dispose();
  });
}

const boards = new WeakMap();

/** Crea la mesa dentro del contenedor y la adapta a su tamaño. */
export function create(element) {
  dispose(element);
  const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  element.appendChild(renderer.domElement);

  const scene = new THREE.Scene();
  light(scene, renderer);

  const camera = new THREE.PerspectiveCamera(38, 1, 0.1, 100);
  const board = { element, renderer, scene, camera, meshes: [], depth: TABLE_WIDTH * 0.75, playing: null, particles: [], stains: [], effects: [] };

  const resize = () => {
    const width = element.clientWidth || 300;
    const height = Math.round(Math.min(width * 0.75, 420));
    renderer.setSize(width, height, false);
    renderer.domElement.style.width = '100%';
    renderer.domElement.style.height = `${height}px`;
    camera.aspect = width / height;
    board.depth = TABLE_WIDTH / camera.aspect;
    const halfFov = THREE.MathUtils.degToRad(camera.fov / 2);
    // Margen para que los dados pegados a las paredes no queden cortados por la perspectiva.
    const distance = (board.depth / 2) / Math.tan(halfFov) * 1.3;
    camera.position.set(0, distance, board.depth * 0.12);
    camera.lookAt(0, 0, 0);
    camera.updateProjectionMatrix();
    renderer.render(scene, camera);
  };
  board.resize = resize;
  board.observer = new ResizeObserver(resize);
  board.observer.observe(element);
  resize();
  boards.set(element, board);
}

function clear(board) {
  for (const mesh of board.meshes) {
    board.scene.remove(mesh);
    disposeDie(mesh);
  }
  board.meshes = [];
  clearParticles(board);
  for (const stain of board.stains) {
    board.scene.remove(stain);
    stain.geometry.dispose();
    stain.material.dispose();
  }
  board.stains = [];
}

// ---------------------------------------------------------------------------------------------
// Efectos al penetrar (F008, research §4): partículas en el dado que penetró
// ---------------------------------------------------------------------------------------------

/** Mancha blanca difuminada, base de auras y partículas. */
function glowTexture() {
  return canvasTexture('glow', 128, (g, s) => {
    const gradient = g.createRadialGradient(s / 2, s / 2, 0, s / 2, s / 2, s / 2);
    gradient.addColorStop(0, 'rgba(255,255,255,1)');
    gradient.addColorStop(0.4, 'rgba(255,255,255,0.55)');
    gradient.addColorStop(1, 'rgba(255,255,255,0)');
    g.fillStyle = gradient;
    g.fillRect(0, 0, s, s);
  });
}

/** Cómo es cada efecto: cuántas partículas, de qué color, hacia dónde y si se suman a la luz. */
const EFFECT_STYLES = {
  Explosion: { count: 70, additive: true, gravity: 0, make: random => ({ hue: 0.05 + random() * 0.08, light: 0.5 + random() * 0.3, size: 0.25 + random() * 0.45, velocity: new THREE.Vector3(random() - 0.5, random() * 0.9, random() - 0.5).normalize().multiplyScalar(2.5 + random() * 4) }) },
  Fire: { count: 80, additive: true, gravity: -3, make: random => ({ hue: 0.02 + random() * 0.09, light: 0.45 + random() * 0.25, size: 0.3 + random() * 0.4, velocity: new THREE.Vector3((random() - 0.5) * 1.2, 1.5 + random() * 3, (random() - 0.5) * 1.2) }) },
  Blood: { count: 45, additive: false, gravity: 18, make: random => ({ hue: 0.99, light: 0.16 + random() * 0.14, size: 0.08 + random() * 0.18, velocity: new THREE.Vector3(random() - 0.5, 0.6 + random() * 0.8, random() - 0.5).normalize().multiplyScalar(3 + random() * 3) }) },
  Flash: { count: 1, additive: true, gravity: 0, make: () => ({ hue: 0.11, light: 0.7, size: 3, velocity: new THREE.Vector3() }) },
};

/** Lanza un efecto en una posición de la mesa. */
function spawnEffect(board, effect, position, now) {
  const style = EFFECT_STYLES[effect];
  const random = Math.random;
  for (let i = 0; i < style.count; i++) {
    const p = style.make(random);
    const sprite = new THREE.Sprite(new THREE.SpriteMaterial({
      map: glowTexture(),
      color: new THREE.Color().setHSL(p.hue, 1, p.light),
      blending: style.additive ? THREE.AdditiveBlending : THREE.NormalBlending,
      transparent: true,
      depthWrite: false,
    }));
    sprite.position.copy(position);
    sprite.scale.setScalar(p.size);
    sprite.userData = { velocity: p.velocity, born: now, size: p.size, gravity: style.gravity, effect };
    board.scene.add(sprite);
    board.particles.push(sprite);
  }
}

/** Mueve y apaga las partículas; la sangre que toca la mesa deja una mancha. */
function updateParticles(board, now) {
  board.particles = board.particles.filter(sprite => {
    const { velocity, born, size, gravity, effect, last } = sprite.userData;
    const age = (now - born) / 1000;
    const dt = Math.min(((now - (last ?? born)) / 1000), 0.05);
    sprite.userData.last = now;
    if (age >= EFFECT_SECONDS) {
      board.scene.remove(sprite);
      sprite.material.dispose();
      return false;
    }
    velocity.y -= gravity * dt;
    sprite.position.addScaledVector(velocity, dt);
    const fade = 1 - age / EFFECT_SECONDS;
    sprite.material.opacity = fade;
    sprite.scale.setScalar(effect === 'Flash' ? size * (0.6 + age * 2) : size * (effect === 'Fire' ? 1 + age : 1));
    if (effect === 'Blood' && sprite.position.y <= 0.02) {
      stain(board, sprite.position, size);
      board.scene.remove(sprite);
      sprite.material.dispose();
      return false;
    }
    return true;
  });
}

function stain(board, position, size) {
  const mark = new THREE.Mesh(new THREE.CircleGeometry(size * (0.8 + Math.random()), 14), new THREE.MeshBasicMaterial({ color: '#5e0a10', transparent: true, opacity: 0.9, depthWrite: false }));
  mark.rotation.x = -Math.PI / 2;
  mark.position.set(position.x, 0.01, position.z);
  board.scene.add(mark);
  board.stains.push(mark);
}

function clearParticles(board) {
  for (const sprite of board.particles) {
    board.scene.remove(sprite);
    sprite.material.dispose();
  }
  board.particles = [];
}

/** Efecto que se verá: el elegido, o un destello si la calidad es baja o se pide menos movimiento (FR-018). */
function effectFor(look) {
  if (look.effect === 'None') return null;
  const reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
  return look.quality === 'Low' || reduced ? 'Flash' : look.effect;
}

function place(mesh, frame) {
  mesh.position.set(frame[0], frame[1], frame[2]);
  mesh.quaternion.set(frame[3], frame[4], frame[5], frame[6]);
}

/**
 * Tira el plan en pantalla. La promesa se cumple cuando los dados se paran o cuando se llama a
 * finish. Dura como mucho MAX_PLAYBACK segundos.
 */
export function roll(element, plan, look) {
  const board = boards.get(element);
  if (!board) throw new Error('La mesa de dados no está creada.');
  finish(element);
  clear(board);
  const shownLook = normalizeLook(look);

  const simulationStarted = performance.now();
  const { items, steps, collisions } = run(plan, Math.floor(Math.random() * 2 ** 31), board.depth);
  board.meshes = items.map(entry => {
    const mesh = dieMesh(entry.data, entry.labels, shownLook, Boolean(entry.item.penetration));
    mesh.visible = false;
    mesh.userData.entry = entry;
    board.scene.add(mesh);
    return mesh;
  });

  // Efectos de penetración: uno por dado extra, en su dado padre, cuando el extra aparece (FR-017).
  const effect = effectFor(shownLook);
  const parentOf = entry => (Number.isInteger(entry.item.parent) ? entry.item.parent : null);
  board.effects = effect ? items.filter(entry => parentOf(entry) !== null).map(entry => ({ effect, parent: parentOf(entry) })) : [];

  // Todo lo que tarda preparar la tirada (simulación, mallas y la primera compilación de los
  // materiales) cuenta para los 4 s de FR-004 de F003: la reproducción se acorta lo que tardó.
  for (const mesh of board.meshes) mesh.visible = true; // compile solo mira lo visible
  board.renderer.compile(board.scene, board.camera);
  for (const mesh of board.meshes) mesh.visible = false;
  const setupSeconds = (performance.now() - simulationStarted) / 1000;
  const duration = Math.min(steps * STEP, Math.max(MAX_PLAYBACK - setupSeconds, MIN_PLAYBACK)) * 1000;
  board.sounds = shownLook.sound ? planSounds(items, collisions, steps, duration / 1000, shownLook, effect) : [];
  playSounds(board.sounds);
  return new Promise(resolve => {
    const startedAt = performance.now();
    const show = (frameIndex, animate) => {
      const now = performance.now();
      for (const mesh of board.meshes) {
        const { entry } = mesh.userData;
        const local = frameIndex - entry.start;
        const appeared = !mesh.visible && local >= 0;
        mesh.visible = local >= 0;
        if (mesh.visible) place(mesh, entry.frames[Math.min(local, entry.frames.length - 1)]);
        const parent = parentOf(entry);
        if (animate && appeared && effect && parent !== null && board.meshes[parent]) {
          spawnEffect(board, effect, board.meshes[parent].position, now);
        }
      }
      updateParticles(board, now);
      board.renderer.render(board.scene, board.camera);
    };
    const end = () => {
      clearTimeout(playing.timer);
      clearParticles(board);
      show(steps, false);
      board.playing = null;
      resolve();
    };
    const tick = (now) => {
      if (board.playing !== playing) return;
      const progress = Math.min((now - startedAt) / Math.max(duration, 1), 1);
      if (progress >= 1) { end(); return; }
      show(Math.floor(progress * steps), true);
      playing.frame = requestAnimationFrame(tick);
    };
    const playing = { end, frame: 0, timer: 0 };
    board.playing = playing;
    playing.frame = requestAnimationFrame(tick);
    // Si la página no se ve, el navegador no da fotogramas: el temporizador termina la tirada
    // igualmente, para que al volver ya esté el resultado.
    playing.timer = setTimeout(() => { if (board.playing === playing) { cancelAnimationFrame(playing.frame); end(); } }, duration + 250);
  });
}

/** Termina la tirada en curso al momento, con los dados en su cara final. */
export function finish(element) {
  const board = boards.get(element);
  if (!board?.playing) return;
  cancelAnimationFrame(board.playing.frame);
  board.playing.end();
}

/** Caras de arriba de los dados en pantalla (depuración y pruebas; T016). */
export function shownFaces(element) {
  const board = boards.get(element);
  if (!board) return [];
  return board.meshes.map(mesh => {
    const { entry } = mesh.userData;
    return entry.labels[topSlot(entry.data, mesh.quaternion)];
  });
}

/** Dados en pantalla con su cara de arriba y si son dados extra por penetración (pruebas, FR-006). */
export function shownDice(element) {
  const board = boards.get(element);
  if (!board) return [];
  return board.meshes.map(mesh => {
    const { entry } = mesh.userData;
    return { face: entry.labels[topSlot(entry.data, mesh.quaternion)], penetration: Boolean(entry.item.penetration), ...mesh.userData.look };
  });
}

/** Libera la mesa al salir de la página. */
export function dispose(element) {
  const board = boards.get(element);
  if (!board) return;
  finish(element);
  clear(board);
  board.observer.disconnect();
  board.scene.environment?.dispose();
  board.renderer.dispose();
  // Devuelve el contexto WebGL al navegador: si no, encender y apagar el 3D muchas veces los acumula.
  board.renderer.forceContextLoss();
  board.renderer.domElement.remove();
  boards.delete(element);
}

// ---------------------------------------------------------------------------------------------
// Vista previa del aspecto (F008, FR-005)
// ---------------------------------------------------------------------------------------------

const previews = new WeakMap();

/** Muestra un d20 que gira con ese aspecto. Llamarla otra vez solo cambia el aspecto. */
export function preview(element, look) {
  let view = previews.get(element);
  if (!view) {
    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    const size = Math.min(element.clientWidth || 160, 160);
    renderer.setSize(size, size, false);
    renderer.domElement.style.width = `${size}px`;
    renderer.domElement.style.height = `${size}px`;
    element.appendChild(renderer.domElement);
    const scene = new THREE.Scene();
    light(scene, renderer);
    const camera = new THREE.PerspectiveCamera(30, 1, 0.1, 20);
    camera.position.set(0, 1.2, 4.2);
    camera.lookAt(0, 0, 0);
    view = { renderer, scene, camera, die: null, frame: 0 };
    const spin = (now) => {
      if (view.die) {
        view.die.rotation.set(0.5 + Math.sin(now / 2400) * 0.3, now / 1600, 0.2);
        renderer.render(scene, camera);
      }
      view.frame = requestAnimationFrame(spin);
    };
    view.frame = requestAnimationFrame(spin);
    previews.set(element, view);
  }

  if (view.die) {
    view.scene.remove(view.die);
    disposeDie(view.die);
  }
  const data = shapeData('D20');
  view.die = dieMesh(data, [...SHAPES.D20.labels], normalizeLook(look));
  view.die.scale.setScalar(1.05);
  view.scene.add(view.die);
  view.renderer.render(view.scene, view.camera);
}

/** Para la vista previa y la libera. */
export function stopPreview(element) {
  const view = previews.get(element);
  if (!view) return;
  cancelAnimationFrame(view.frame);
  if (view.die) disposeDie(view.die);
  view.scene.environment?.dispose();
  view.renderer.dispose();
  view.renderer.forceContextLoss();
  view.renderer.domElement.remove();
  previews.delete(element);
}

// ---------------------------------------------------------------------------------------------
// Sonido (F008, research §3)
// ---------------------------------------------------------------------------------------------

let audio = null;

/** Crea o reanuda el contexto de audio; se llama al pulsar «Tirar». Nunca lanza excepciones. */
export function unlockAudio() {
  try {
    const Context = window.AudioContext || window.webkitAudioContext;
    if (!Context) return;
    audio ??= new Context();
    if (audio.state === 'suspended') audio.resume().catch(() => {});
  } catch {
    audio = null;
  }
}

/** Efectos de penetración de la última tirada: [{ effect, parent }] (pruebas, FR-017). */
export function shownEffects(element) {
  return boards.get(element)?.effects ?? [];
}

const MAX_SOUNDS_PER_WINDOW = 4;   // como mucho 4 golpes…
const SOUND_WINDOW = 0.05;         // …cada 50 ms (FR-008)

/**
 * Sonidos de una tirada, en segundos desde que empieza la animación: un golpe por choque (volumen
 * según la fuerza, tipo = material) y el del efecto cuando aparece cada dado extra.
 */
function planSounds(items, collisions, steps, seconds, look, effect) {
  const at = step => (steps > 0 ? (step / steps) * seconds : 0);
  const planned = collisions.map(hit => ({ at: at(hit.step), volume: Math.min(1, hit.strength / 12), kind: look.material }));
  if (effect) {
    for (const entry of items) {
      if (Number.isInteger(entry.item.parent)) planned.push({ at: at(entry.start), volume: 0.8, kind: effect });
    }
  }
  planned.sort((a, b) => a.at - b.at);
  const kept = [];
  for (const sound of planned) {
    const recent = kept.filter(other => sound.at - other.at < SOUND_WINDOW).length;
    if (recent < MAX_SOUNDS_PER_WINDOW || sound.kind === effect) {
      if (recent >= MAX_SOUNDS_PER_WINDOW) kept.splice(kept.findLastIndex(other => other.kind !== effect && sound.at - other.at < SOUND_WINDOW), 1);
      kept.push(sound);
    }
  }
  return kept;
}

/** Tono de cada material: frecuencia del golpe (Hz), cuánto resuena (s) y cuánto ruido lleva. */
const TONES = {
  Ivory: [900, 0.06, 0.8], Marble: [1500, 0.09, 0.6], OldGold: [2300, 0.4, 0.3], BlackIron: [1700, 0.35, 0.35],
  Ruby: [3300, 0.28, 0.3], Obsidian: [2100, 0.14, 0.5], Bone: [650, 0.05, 0.9], Amethyst: [3000, 0.28, 0.3],
  Wood: [320, 0.08, 0.9], Stone: [550, 0.05, 1], Crystal: [3800, 0.35, 0.25],
};

let noiseBuffer = null;

function noise(context) {
  if (noiseBuffer) return noiseBuffer;
  noiseBuffer = context.createBuffer(1, context.sampleRate * 0.8, context.sampleRate);
  const data = noiseBuffer.getChannelData(0);
  for (let i = 0; i < data.length; i++) data[i] = Math.random() * 2 - 1;
  return noiseBuffer;
}

/** Ruido filtrado con envolvente: la base de golpes, estallidos y soplos. */
function burst(context, time, { volume, type, frequency, length, q = 1 }) {
  const source = context.createBufferSource();
  source.buffer = noise(context);
  const filter = context.createBiquadFilter();
  filter.type = type;
  filter.frequency.value = frequency;
  filter.Q.value = q;
  const gain = context.createGain();
  gain.gain.setValueAtTime(volume, time);
  gain.gain.exponentialRampToValueAtTime(0.0001, time + length);
  source.connect(filter).connect(gain).connect(context.destination);
  source.start(time);
  source.stop(time + length + 0.05);
}

/** Tono que resuena y se apaga: el «clic» o el «clinc» del material. */
function ring(context, time, { volume, frequency, length }) {
  const oscillator = context.createOscillator();
  oscillator.frequency.value = frequency * (0.92 + Math.random() * 0.16);
  const gain = context.createGain();
  gain.gain.setValueAtTime(volume, time);
  gain.gain.exponentialRampToValueAtTime(0.0001, time + length);
  oscillator.connect(gain).connect(context.destination);
  oscillator.start(time);
  oscillator.stop(time + length + 0.05);
}

/** Programa los sonidos en el contexto de audio; si no hay audio o falla, no pasa nada (FR-010). */
function playSounds(sounds) {
  if (!audio || sounds.length === 0) return;
  try {
    const start = audio.currentTime + 0.02;
    for (const sound of sounds) {
      const time = start + sound.at;
      const tone = TONES[sound.kind];
      if (tone) {
        const [frequency, length, grit] = tone;
        burst(audio, time, { volume: sound.volume * 0.5 * grit, type: 'bandpass', frequency: frequency * 1.5, length: 0.04 });
        ring(audio, time, { volume: sound.volume * 0.25, frequency, length });
      } else if (sound.kind === 'Explosion') {
        burst(audio, time, { volume: 0.9, type: 'lowpass', frequency: 500, length: 0.6 });
        ring(audio, time, { volume: 0.3, frequency: 70, length: 0.4 });
      } else if (sound.kind === 'Fire') {
        burst(audio, time, { volume: 0.5, type: 'bandpass', frequency: 900, length: 0.7, q: 0.6 });
      } else if (sound.kind === 'Blood') {
        burst(audio, time, { volume: 0.6, type: 'lowpass', frequency: 350, length: 0.25 });
        burst(audio, time + 0.08, { volume: 0.4, type: 'lowpass', frequency: 250, length: 0.2 });
      } else {
        ring(audio, time, { volume: 0.3, frequency: 1800, length: 0.3 });
      }
    }
  } catch {
    // Sin audio la tirada sigue igual.
  }
}

/** Sonidos programados en la última tirada: [{ at, volume, kind }] (pruebas, FR-008). */
export function heardSounds(element) {
  return boards.get(element)?.sounds ?? [];
}
